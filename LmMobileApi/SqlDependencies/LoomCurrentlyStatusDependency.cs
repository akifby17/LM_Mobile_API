using Dapper;
using LmMobileApi.Hubs;
using LmMobileApi.Looms.Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;

namespace LmMobileApi.SqlDependencies;

public class LoomCurrentlyStatusDependency : IDisposable
{
    //son
    private readonly string _connectionString;
    private readonly IHubContext<LoomsCurrentlyStatusHub> _hubContext;
    private readonly ILogger<LoomCurrentlyStatusDependency> _logger;
    private readonly LoomFilter _filter;
    private readonly string _connectionId; // Hangi client için olduğunu belirtmek için

    private SqlDependency? _sqlDependency;
    private SqlCommand? _command;
    private SqlConnection? _connection;
    private List<Loom> _looms = [];
    private bool _disposed = false;

    // Filtreleme için hazırlanmış sorgu parçaları
    private string _filteredQuery = string.Empty;
    private DynamicParameters _queryParameters = new();

    public LoomCurrentlyStatusDependency(
        IConfiguration configuration,
        IHubContext<LoomsCurrentlyStatusHub> hubContext,
        ILogger<LoomCurrentlyStatusDependency> logger,
        LoomFilter filter,
        string connectionId = "") // Client ID'si için
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _hubContext = hubContext;
        _logger = logger;
        _filter = filter;
        _connectionId = connectionId; // Client-specific bildirimler için

        // SQL Server broker servisini başlatma
        try
        {
            SqlDependency.Start(_connectionString);
            _logger.LogInformation("SQL Dependency started with filter {@Filter} for connection {ConnectionId}",
                filter, connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SQL Dependency service");
            throw;
        }

        // Filtreleme sorgusunu önceden hazırla
        PrepareFilteredQuery();
    }
    /*
     
     var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(filter.EventNameTR))
            {
                whereClauses.Add("EventID = @EventNameTR");
                parameters.Add("@EventNameTR", filter.EventNameTR);
            }
            if (!string.IsNullOrEmpty(filter.ModelName))
            {
                whereClauses.Add("ModelName =@ModelName");
                parameters.Add("@ModelName", filter.ModelName);
            }
            if (!string.IsNullOrEmpty(filter.MarkName))
            {
                whereClauses.Add("MarkName = @MarkName");
                parameters.Add("@MarkName", filter.MarkName);
            }
            if (!string.IsNullOrEmpty(filter.GroupName))
            {
                whereClauses.Add("GroupName = @GroupName");
                parameters.Add("@GroupName", filter.GroupName);
            }
            if (!string.IsNullOrEmpty(filter.HallName))
            {
                whereClauses.Add("HallName = @HallName");
                parameters.Add("@HallName", filter.HallName);
            }
            if (!string.IsNullOrEmpty(filter.ClassName))
            {
                whereClauses.Add("ClassName = @ClassName");
                parameters.Add("@ClassName", filter.ClassName);
            }
     
     
     */
    /// <summary>
    /// Filter'a göre SQL sorgusu ve parametrelerini hazırlar
    /// </summary>
    private void PrepareFilteredQuery()
    {
        var whereClauses = new List<string>();
        _queryParameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(_filter.EventNameTR))
        {
            whereClauses.Add("EventID = @EventNameTR");
            _queryParameters.Add("@EventNameTR", _filter.EventNameTR);
        }

        if (!string.IsNullOrEmpty(_filter.ModelName))
        {
            whereClauses.Add("ModelName =@ModelName");
            _queryParameters.Add("@ModelName", _filter.ModelName);
        }

        if (!string.IsNullOrEmpty(_filter.MarkName))
        {
            whereClauses.Add("MarkName = @MarkName");
            _queryParameters.Add("@MarkName", _filter.MarkName);
        }

        if (!string.IsNullOrEmpty(_filter.GroupName))
        {
            whereClauses.Add("GroupName = @GroupName");
            _queryParameters.Add("@GroupName", _filter.GroupName);
        }

        if (!string.IsNullOrEmpty(_filter.ClassName))
        {
            whereClauses.Add("ClassName = @ClassName");
            _queryParameters.Add("@ClassName", _filter.ClassName);
        }

        // HallName filtresi eklendi (eksikti)
        if (!string.IsNullOrEmpty(_filter.HallName))
        {
            whereClauses.Add("HallName = @HallName");
            _queryParameters.Add("@HallName", _filter.HallName);
        }

        // Filtrelenmiş sorguyu hazırla
        var queryBuilder = new StringBuilder("SELECT * FROM tvw_mobile_Looms_CurrentlyStatus");
        if (whereClauses.Any())
        {
            queryBuilder.Append(" WHERE " + string.Join(" AND ", whereClauses));
        }
        queryBuilder.Append(" ORDER BY LoomNo");

        _filteredQuery = queryBuilder.ToString();

        _logger.LogDebug("Prepared filtered query: {Query} with {ParamCount} parameters",
            _filteredQuery, _queryParameters.ParameterNames.Count());
    }

    public async Task StartListening()
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot start listening on disposed dependency");
            return;
        }

        try
        {
            CleanupConnection();

            _connection = new SqlConnection(_connectionString);
            await _connection.OpenAsync();

            // Dependency için temel tablodan dinleme sorgusu (view değil, tablo)
            var dependencyQuery = BuildDependencyQuery();

            _command = new SqlCommand(dependencyQuery, _connection)
            {
                CommandType = CommandType.Text,
                Notification = null
            };

            // Parametreleri ekle
            foreach (var name in _queryParameters.ParameterNames)
            {
                _command.Parameters.AddWithValue(name, _queryParameters.Get<object>(name)!);
            }

            _sqlDependency = new SqlDependency(_command);
            _sqlDependency.OnChange += SqlDependency_OnChange;

            // Dependency'yi başlatmak için sorguyu çalıştır
            using var reader = await _command.ExecuteReaderAsync();
            reader.Close();

            // İlk veriyi yükle
            await LoadInitialFilteredData();

            _logger.LogInformation("SQL Dependency listening started with filter, loaded {Count} looms", _looms.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while starting SQL Dependency listening");
            CleanupConnection();
        }
    }

    /// <summary>
    /// SqlDependency için temel tablo sorgusunu oluşturur
    /// </summary>
    private string BuildDependencyQuery()
    {
        var whereClauses = new List<string>();

        if (!string.IsNullOrEmpty(_filter.EventNameTR))
        {
            whereClauses.Add("EventID = @EventNameTR");

        }

        if (!string.IsNullOrEmpty(_filter.ModelName))
        {
            whereClauses.Add("ModelName =@ModelName");

        }

        if (!string.IsNullOrEmpty(_filter.MarkName))
        {
            whereClauses.Add("MarkName = @MarkName");

        }

        if (!string.IsNullOrEmpty(_filter.GroupName))
        {
            whereClauses.Add("GroupName = @GroupName");

        }

        if (!string.IsNullOrEmpty(_filter.ClassName))
        {
            whereClauses.Add("ClassName = @ClassName");

        }

        // HallName filtresi eklendi (eksikti)
        if (!string.IsNullOrEmpty(_filter.HallName))
        {
            whereClauses.Add("HallName = @HallName");
        }

        var query = new StringBuilder(
            "SELECT LoomNo, EventID, LoomSpeed, PID, WID, OperationCode, ShiftNo, ShiftPickCounter, StyleWorkOrderNo, WarpWorkOrderNo " +
            "FROM dbo.Looms_CurrentlyStatus"
        );

        if (whereClauses.Any())
            query.Append(" WHERE " + string.Join(" AND ", whereClauses));

        return query.ToString();
    }

    /// <summary>
    /// Filtrelenmiş ilk veriyi yükler
    /// </summary>
    private async Task LoadInitialFilteredData()
    {
        try
        {
            if (_connection?.State != ConnectionState.Open)
            {
                _connection = new SqlConnection(_connectionString);
                await _connection.OpenAsync();
            }

            _looms = _connection.Query<Loom>(_filteredQuery, _queryParameters).AsList();

            _logger.LogDebug("Loaded {Count} filtered looms for connection {ConnectionId}",
                _looms.Count, _connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading initial filtered data");
            throw;
        }
    }

    private async void SqlDependency_OnChange(object sender, SqlNotificationEventArgs e)
    {
        _logger.LogInformation("SqlDependency_OnChange(): Type={Type}, Info={Info}, Source={Source}",
                   e.Type, e.Info, e.Source);
        if (_disposed)
        {
            _logger.LogDebug("Ignoring SQL dependency change event on disposed instance");
            return;
        }

        if (_sqlDependency != null)
        {
            _sqlDependency.OnChange -= SqlDependency_OnChange;
        }

        try
        {
            if (e.Type == SqlNotificationType.Change && e.Info != SqlNotificationInfo.Invalid)
            {
                await HandleFilteredDataChange();
            }
            else
            {
                _logger.LogInformation("SqlDependency_OnChange(): Type={Type}, Info={Info}, Source={Source}",
                    e.Type, e.Info, e.Source);

            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling SQL dependency change");
        }
        finally
        {
            if (!_disposed)
            {
                await Task.Delay(1000);
                StartListening();
            }
        }
    }

    /// <summary>
    /// Sadece filtrelenmiş verilerdeki değişiklikleri işler
    /// </summary>
    private async Task HandleFilteredDataChange()
    {
        try
        {
            if (_connection?.State != ConnectionState.Open)
            {
                _connection?.Close();
                _connection = new SqlConnection(_connectionString);
                await _connection.OpenAsync();
            }

            // Yeni filtrelenmiş tüm looms
            var currentFilteredLooms = _connection.Query<Loom>(_filteredQuery, _queryParameters).AsList();

            // Mevcut listeyle karşılaştırıp değişiklikleri tespit et
            var changes = DetectChanges(_looms, currentFilteredLooms);

            if (changes.HasChanges)
            {
                _logger.LogInformation(
                    "Detected changes for connection {ConnectionId}: {AddedCount} added, {UpdatedCount} updated, {RemovedCount} removed",
                    _connectionId, changes.Added.Count, changes.Updated.Count, changes.Removed.Count);

                // —— Filtre listesini tekrar oluşturuyoruz ——
                var filters = new List<FilterOption>
            {
                new FilterOption {
                    Key = "hallName",
                    Values = currentFilteredLooms.Select(x => x.HallName).Distinct().OrderBy(x => x)
                },
                new FilterOption {
                    Key = "markName",
                    Values = currentFilteredLooms.Select(x => x.MarkName).Distinct().OrderBy(x => x)
                },
                new FilterOption {
                    Key = "groupName",
                    Values = currentFilteredLooms.Select(x => x.GroupName).Distinct().OrderBy(x => x)
                },
                new FilterOption {
                    Key = "modelName",
                    Values = currentFilteredLooms.Select(x => x.ModelName).Distinct().OrderBy(x => x)
                }
            };

                // Hedef client
                var targetClient = string.IsNullOrEmpty(_connectionId)
                    ? _hubContext.Clients.All
                    : _hubContext.Clients.Client(_connectionId);

                // Her bir event'i hem ilgili loom’ları hem de filtreleri içerecek şekilde paketleyelim:
                if (changes.Added.Any())
                    await targetClient.SendAsync("LoomsAdded", new
                    {
                        looms = changes.Added,
                        filters = filters
                    });

                if (changes.Updated.Any())
                    await targetClient.SendAsync("LoomsUpdated", new
                    {
                        looms = changes.Updated,
                        filters = filters
                    });

                if (changes.Removed.Any())
                    await targetClient.SendAsync("LoomsRemoved", new
                    {
                        looms = changes.Removed.Select(l => l.LoomNo).ToList(),
                        filters = filters
                    });
            }

            // Yeni durumu bir sonraki tetikleme için sakla
            _looms = currentFilteredLooms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling filtered data change");
        }
    }


    /// <summary>
    /// İki liste arasındaki değişiklikleri tespit eder
    /// </summary>
    private ChangeDetectionResult DetectChanges(List<Loom> oldLooms, List<Loom> newLooms)
    {
        var result = new ChangeDetectionResult();

        // Eklenen kayıtlar
        result.Added = newLooms.Where(n => !oldLooms.Any(o => o.LoomNo == n.LoomNo)).ToList();

        // Silinen kayıtlar
        result.Removed = oldLooms.Where(o => !newLooms.Any(n => n.LoomNo == o.LoomNo)).ToList();

        // Güncellenmiş kayıtlar
        result.Updated = newLooms.Where(n =>
            oldLooms.Any(o => o.LoomNo == n.LoomNo && !o.Equals(n))).ToList();

        return result;
    }

    private void CleanupConnection()
    {
        try
        {
            if (_sqlDependency != null)
            {
                _sqlDependency.OnChange -= SqlDependency_OnChange;
                _sqlDependency = null;
            }

            _command?.Dispose();
            _command = null;

            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while cleaning up connection");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _disposed = true;
            CleanupConnection();

            // SqlDependency.Stop sadece son instance dispose edildiğinde çağrılmalı
            // Bu implementation'da her client için ayrı dependency var, 
            // bu yüzden global counter veya manager gerekebilir

            _logger.LogInformation("SQL Dependency disposed for connection {ConnectionId}", _connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during disposal");
        }
    }
}

/// <summary>
/// Değişiklik tespiti sonuçları
/// </summary>
public class ChangeDetectionResult
{
    public List<Loom> Added { get; set; } = new();
    public List<Loom> Updated { get; set; } = new();
    public List<Loom> Removed { get; set; } = new();

    public bool HasChanges => Added.Any() || Updated.Any() || Removed.Any();
}