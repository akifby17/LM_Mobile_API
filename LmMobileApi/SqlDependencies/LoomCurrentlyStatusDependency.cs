using Dapper;
using LmMobileApi.Hubs;
using LmMobileApi.Looms.Domain;
using LmMobileApi.Looms.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Data;

namespace LmMobileApi.SqlDependencies;

public class LoomCurrentlyStatusDependency : IDisposable
{
    
    private readonly string _connectionString;
    private readonly IHubContext<LoomsCurrentlyStatusHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private SqlDependency? _sqlDependency;
    private SqlCommand? _command;
    private SqlConnection? _connection;
    private List<Loom> _looms = [];

    public LoomCurrentlyStatusDependency(
        IConfiguration configuration,
        IHubContext<LoomsCurrentlyStatusHub> hubContext,
        IServiceProvider serviceProvider)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;

        // SQL Server broker servisini başlatma
        SqlDependency.Start(_connectionString);
    }

    public void StartListening()
    {
        try
        {
            // Bağlantı ve komut oluşturma
            _connection = new SqlConnection(_connectionString);
            _connection.Open();

            _command = new SqlCommand(
                "SELECT LoomNo, EventID, LoomSpeed, PID, WID, OperationCode, ShiftNo, ShiftPickCounter, StyleWorkOrderNo, WarpWorkOrderNo FROM dbo.Looms_CurrentlyStatus",
                _connection)
            {
                CommandType = CommandType.Text,
                Notification = null
            };

            // Dependency oluşturma ve dinlemeye başlama
            _sqlDependency = new SqlDependency(_command);
            _sqlDependency.OnChange += SqlDependency_OnChange;

            // Sorguyu çalıştır (dependency başlatmak için)
            using var reader = _command.ExecuteReader();

            // Şu anki durumu al
            _looms = _connection.Query<Loom>("SELECT * FROM tvw_mobile_Looms_CurrentlyStatus ORDER BY LoomNo").AsList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQL Dependency başlatılırken hata oluştu: {ex.Message}");
        }
    }

    private async void SqlDependency_OnChange(object sender, SqlNotificationEventArgs e)
    {
        // Bu olay handler'ı sadece bir kez çalışır, yeni dinleyici kurmak gerekiyor
        if (_sqlDependency != null)
        {
            _sqlDependency.OnChange -= SqlDependency_OnChange;
        }

        try
        {
            if (e.Type == SqlNotificationType.Change && e.Info != SqlNotificationInfo.Invalid)
            {
                List<Loom> currentLooms = _connection!.Query<Loom>("SELECT * FROM tvw_mobile_Looms_CurrentlyStatus ORDER BY LoomNo").ToList();
                List<Loom> changedLooms = new();

                // Değişen loom'ları tespit et
                foreach (Loom currentLoom in currentLooms)
                {
                    Loom? findLoom = _looms.Find(x => x.LoomNo == currentLoom.LoomNo);
                    
                    if (findLoom != null && !currentLoom.Equals(findLoom))
                    {
                        changedLooms.Add(currentLoom);
                    }
                    else if (findLoom == null)
                    {
                        // Yeni loom eklendi
                        changedLooms.Add(currentLoom);
                    }
                }

                // Değişiklikleri SignalR üzerinden gönder
                if (changedLooms.Any())
                {
                    // Değişen loom'ları + filtre seçeneklerini birlikte gönder
                    await SendChangedLoomsWithFilters(changedLooms);
                }

                // Cache'i güncelle
                _looms = currentLooms;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR bildirimi gönderilirken hata: {ex.Message}");
        }

        // Yeniden dinlemeye başla
        StartListening();
    }

    private async Task SendChangedLoomsWithFilters(List<Loom> changedLooms)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var loomRepository = scope.ServiceProvider.GetRequiredService<ILoomRepository>();
            
            // Filtre seçeneklerini al
            var filterOptionsResult = await loomRepository.GetFilterOptionsAsync();
            if (!filterOptionsResult.IsSuccess)
            {
                Console.WriteLine($"❌ Filtre seçenekleri alınırken hata: {filterOptionsResult.Error}");
                return;
            }

            // 1. Filtresiz kullanıcılara tüm değişen loom'lar + filtreler gönder
            var allLoomsData = new LoomsWithFilters
            {
                looms = changedLooms,
                filters = filterOptionsResult.Data!
            };
            await _hubContext.Clients.Group("all").SendAsync("FilteredLoomsDataChanged", allLoomsData);

            // 2. Her aktif filtre grubu için filtreye uyan değişen loom'lar + filtreler gönder
            var activeGroups = LoomsCurrentlyStatusHub.ConnectionFilters.Values.Distinct().ToList();
            
            foreach (var filter in activeGroups)
            {
                // Bu filtreye uyan değişen loom'ları bul
                var filteredChangedLooms = changedLooms.Where(loom => LoomMatchesFilter(loom, filter)).ToList();
                
                if (filteredChangedLooms.Any())
                {
                    var filteredLoomsData = new LoomsWithFilters
                    {
                        looms = filteredChangedLooms,
                        filters = filterOptionsResult.Data!
                    };

                    var groupName = GetGroupNameFromFilter(filter);
                    await _hubContext.Clients.Group(groupName).SendAsync("FilteredLoomsDataChanged", filteredLoomsData);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SendChangedLoomsWithFilters error: {ex.Message}");
        }
    }



    // Loom'un filtreye uygun olup olmadığını kontrol et (Hub'dan kopyalandı)
    private static bool LoomMatchesFilter(Loom loom, LoomFilter filter)
    {
        if (!string.IsNullOrEmpty(filter.HallName) && loom.HallName != filter.HallName)
            return false;
        if (!string.IsNullOrEmpty(filter.MarkName) && loom.MarkName != filter.MarkName)
            return false;
        if (!string.IsNullOrEmpty(filter.GroupName) && loom.GroupName != filter.GroupName)
            return false;
        if (!string.IsNullOrEmpty(filter.ModelName) && loom.ModelName != filter.ModelName)
            return false;
        if (!string.IsNullOrEmpty(filter.ClassName) && loom.ClassName != filter.ClassName)
            return false;
        if (!string.IsNullOrEmpty(filter.EventNameTR) && loom.EventNameTR != filter.EventNameTR)
            return false;

        return true;
    }

    // Filtreden grup adı oluştur (Hub'dan kopyalandı)
    private static string GetGroupNameFromFilter(LoomFilter filter)
    {
        var filterParts = new List<string>();
        
        if (!string.IsNullOrEmpty(filter.HallName))
            filterParts.Add($"hall:{filter.HallName}");
        if (!string.IsNullOrEmpty(filter.MarkName))
            filterParts.Add($"mark:{filter.MarkName}");
        if (!string.IsNullOrEmpty(filter.GroupName))
            filterParts.Add($"group:{filter.GroupName}");
        if (!string.IsNullOrEmpty(filter.ModelName))
            filterParts.Add($"model:{filter.ModelName}");
        if (!string.IsNullOrEmpty(filter.ClassName))
            filterParts.Add($"class:{filter.ClassName}");
        if (!string.IsNullOrEmpty(filter.EventNameTR))
            filterParts.Add($"event:{filter.EventNameTR}");

        return filterParts.Count == 0 ? "all" : string.Join("|", filterParts);
    }

    // Bu method artık kullanılmıyor - SendChangedLoomsWithFilters her şeyi birlikte gönderiyor
    [Obsolete("Use SendChangedLoomsWithFilters instead")]
    private async Task SendFilterOptionsUpdate()
    {
        // Artık kullanılmıyor, SendChangedLoomsWithFilters method'u hem değişen loom'ları
        // hem de filtre seçeneklerini birlikte FilteredLoomsDataChanged event'i ile gönderiyor
    }



    public void Dispose()
    {
        if (_sqlDependency != null)
            _sqlDependency.OnChange -= SqlDependency_OnChange;
        _connection?.Close();
        _connection?.Dispose();
        SqlDependency.Stop(_connectionString);
    }
}

