using Dapper;
using LmMobileApi.Looms.Domain;
using LmMobileApi.SqlDependencies;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace LmMobileApi.Hubs
{
    /// <summary>
    /// Tezgah verisini filtrelemek için kullanılacak model.
    /// </summary>
    public class LoomFilter
    {
        public int? EventId { get; set; }
        public int? MinSpeed { get; set; }
        public int? MaxSpeed { get; set; }
        public string? StyleWorkOrderNo { get; set; }
        public string? HallName { get; set; }

        /// <summary>
        /// Filter'ın eşitlik kontrolü için
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not LoomFilter other) return false;

            return EventId == other.EventId &&
                   MinSpeed == other.MinSpeed &&
                   MaxSpeed == other.MaxSpeed &&
                   StyleWorkOrderNo == other.StyleWorkOrderNo &&
                   HallName == other.HallName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EventId, MinSpeed, MaxSpeed, StyleWorkOrderNo, HallName);
        }
    }

    public class LoomsCurrentlyStatusHub : Hub
    {
        // Her client için dependency örneklerini saklamak üzere
        private static readonly ConcurrentDictionary<string, ClientSubscription> _clientSubscriptions
            = new ConcurrentDictionary<string, ClientSubscription>();

        private readonly IConfiguration _configuration;
        private readonly IHubContext<LoomsCurrentlyStatusHub> _hubContext;
        private readonly ILogger<LoomCurrentlyStatusDependency> _logger;
        private readonly Func<LoomFilter, string, LoomCurrentlyStatusDependency> _depFactory;

        public LoomsCurrentlyStatusHub(
            IConfiguration configuration,
            IHubContext<LoomsCurrentlyStatusHub> hubContext,
            ILogger<LoomCurrentlyStatusDependency> logger,
            Func<LoomFilter, string, LoomCurrentlyStatusDependency> depFactory) // ConnectionId parametresi eklendi
        {
            _configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration));
            _hubContext = hubContext
                ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
            _depFactory = depFactory
                ?? throw new ArgumentNullException(nameof(depFactory));
        }

        /// <summary>
        /// Client, istediği filtreyi vererek bu metodu çağırır.
        /// Aynı filtreye sahipse yeniden oluşturulmaz.
        /// </summary>
        public async Task Subscribe(LoomFilter? filter)
        {
            filter ??= new LoomFilter();

            // Mevcut subscription'ı kontrol et
            if (_clientSubscriptions.TryGetValue(Context.ConnectionId, out var existingSubscription))
            {
                // Aynı filter ise işlem yapma
                if (existingSubscription.Filter.Equals(filter))
                {
                    _logger.LogDebug("Client {ConnectionId} already subscribed with same filter", Context.ConnectionId);

                    // Yine de mevcut veriyi gönder (client yeniden bağlanmış olabilir)
                    await GetInitialData(filter);
                    return;
                }

                // Farklı filter ise eski subscription'ı temizle
                existingSubscription.Dependency.Dispose();
                _clientSubscriptions.TryRemove(Context.ConnectionId, out _);

                _logger.LogInformation("Updated subscription for ConnectionId={ConnId} from {@OldFilter} to {@NewFilter}",
                    Context.ConnectionId, existingSubscription.Filter, filter);
            }

            // Factory ile yeni dependency örneği oluştur (ConnectionId ile)
            var newDep = _depFactory(filter, Context.ConnectionId);
            var newSubscription = new ClientSubscription
            {
                Filter = filter,
                Dependency = newDep,
                SubscriptionTime = DateTime.UtcNow
            };

            _clientSubscriptions[Context.ConnectionId] = newSubscription;

            // Dependency'yi başlat
            await newDep.StartListening();

            _logger.LogInformation(
                "Started subscription for ConnectionId={ConnId} with filter {@Filter}",
                Context.ConnectionId, filter
            );

            // İlk veriyi gönder
            await GetInitialData(filter);
        }

        /// <summary>
        /// Client'ın mevcut filtresini döndürür
        /// </summary>
        public async Task<LoomFilter?> GetCurrentFilter()
        {
            if (_clientSubscriptions.TryGetValue(Context.ConnectionId, out var subscription))
            {
                await Clients.Caller.SendAsync("CurrentFilter", subscription.Filter);
                return subscription.Filter;
            }

            await Clients.Caller.SendAsync("CurrentFilter", null);
            return null;
        }

        /// <summary>
        /// Filtrelenmiş ilk veriyi gönderir
        /// </summary>
        public async Task GetInitialData(LoomFilter? filter = null)
        {
            try
            {
                filter ??= new LoomFilter();

                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                var (query, parameters) = BuildFilteredQuery(filter);
                var looms = connection.Query<Loom>(query, parameters).AsList();

                // Caller'a filtrelenmiş verileri gönder
                await Clients.Caller.SendAsync("InitialLoomsData", looms);

                _logger.LogInformation(
                    "Sent {Count} initial filtered looms to ConnectionId={ConnId}",
                    looms.Count, Context.ConnectionId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting initial data for ConnectionId={ConnId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Failed to get initial data");
            }
        }

        /// <summary>
        /// Filter'a göre SQL sorgusu ve parametrelerini oluşturur
        /// </summary>
        private (string query, DynamicParameters parameters) BuildFilteredQuery(LoomFilter filter)
        {
            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (filter.EventId.HasValue)
            {
                whereClauses.Add("EventID = @EventId");
                parameters.Add("@EventId", filter.EventId.Value);
            }
            if (filter.MinSpeed.HasValue)
            {
                whereClauses.Add("LoomSpeed >= @MinSpeed");
                parameters.Add("@MinSpeed", filter.MinSpeed.Value);
            }
            if (filter.MaxSpeed.HasValue)
            {
                whereClauses.Add("LoomSpeed <= @MaxSpeed");
                parameters.Add("@MaxSpeed", filter.MaxSpeed.Value);
            }
            if (!string.IsNullOrEmpty(filter.StyleWorkOrderNo))
            {
                whereClauses.Add("StyleWorkOrderNo = @StyleNo");
                parameters.Add("@StyleNo", filter.StyleWorkOrderNo);
            }
            if (!string.IsNullOrEmpty(filter.HallName))
            {
                whereClauses.Add("HallName = @HallName");
                parameters.Add("@HallName", filter.HallName);
            }

            var query = new StringBuilder("SELECT * FROM tvw_mobile_Looms_CurrentlyStatus");
            if (whereClauses.Any())
                query.Append(" WHERE " + string.Join(" AND ", whereClauses));
            query.Append(" ORDER BY LoomNo");

            return (query.ToString(), parameters);
        }

        /// <summary>
        /// Client'ın subscription'ını durdurur
        /// </summary>
        public async Task Unsubscribe()
        {
            if (_clientSubscriptions.TryRemove(Context.ConnectionId, out var subscription))
            {
                subscription.Dependency.Dispose();
                _logger.LogInformation("Unsubscribed ConnectionId={ConnId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Unsubscribed");
            }
        }

        /// <summary>
        /// Aktif subscription sayısını döndürür (debug amaçlı)
        /// </summary>
        public async Task GetActiveSubscriptionCount()
        {
            var count = _clientSubscriptions.Count;
            await Clients.Caller.SendAsync("ActiveSubscriptionCount", count);

            _logger.LogInformation("Active subscription count: {Count}", count);
        }

        /// <summary>
        /// Client bağlantısı kapandığında ilgili dependency temizlenir.
        /// </summary>
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (_clientSubscriptions.TryRemove(Context.ConnectionId, out var subscription))
            {
                subscription.Dependency.Dispose();
                _logger.LogInformation(
                    "Disposed subscription for ConnectionId={ConnId} on disconnect. Subscription duration: {Duration}",
                    Context.ConnectionId, DateTime.UtcNow - subscription.SubscriptionTime
                );
            }

            return base.OnDisconnectedAsync(exception);
        }
    }

    /// <summary>
    /// Client subscription bilgilerini tutan model
    /// </summary>
    public class ClientSubscription
    {
        public LoomFilter Filter { get; set; } = new();
        public LoomCurrentlyStatusDependency Dependency { get; set; } = null!;
        public DateTime SubscriptionTime { get; set; }
    }
}