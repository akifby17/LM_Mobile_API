using LmMobileApi.Looms.Domain;
using LmMobileApi.Looms.Application.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace LmMobileApi.Hubs;
[Authorize]
public class LoomsCurrentlyStatusHub : Hub
{
    // Client'ların abone olduğu filtreleri takip etmek için - Thread-safe
    private static readonly ConcurrentDictionary<string, LoomFilter> _connectionFilters = new();
    
    // External access için
    public static ConcurrentDictionary<string, LoomFilter> ConnectionFilters => _connectionFilters;

    // Bu method artık kullanılmıyor - SendLoomChangeToFilteredGroups kullan
    [Obsolete("Use SendLoomChangeToFilteredGroups instead")]
    public async Task SendLoomsCurrentlyStatus(Loom loom)
    {
        await SendLoomChangeToFilteredGroups(loom);
    }

    // Filtreli loom verilerini gönder
    public async Task SendFilteredLoomsData(LoomsWithFilters data, LoomFilter? appliedFilter = null)
    {
        if (appliedFilter == null)
        {
            // Filtre yoksa "all" grubuna gönder (filtresiz kullanıcılar)
            await Clients.Group("all").SendAsync("FilteredLoomsDataChanged", data);
        }
        else
        {
            // Belirli filtreye sahip grup'a gönder
            var groupName = GetGroupNameFromFilter(appliedFilter);
            await Clients.Group(groupName).SendAsync("FilteredLoomsDataChanged", data);
        }
    }

    // Client filtre aboneliği yapar
    public async Task SubscribeToFilter(string filterJson)
    {
        try
        {
            var filter = JsonSerializer.Deserialize<LoomFilter>(filterJson);
            if (filter != null)
            {
                // Eski grup'tan çık
                if (_connectionFilters.TryGetValue(Context.ConnectionId, out var oldFilter))
                {
                    var oldGroupName = GetGroupNameFromFilter(oldFilter);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, oldGroupName);
                }
                else
                {
                    // "all" grubundan çık (filtresiz kullanıcılar grubu)
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all");
                }

                // Yeni gruba katıl
                var groupName = GetGroupNameFromFilter(filter);
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                
                // Connection'ın filtresi olarak kaydet
                _connectionFilters[Context.ConnectionId] = filter;

                // İLK SEFERDE filtreye uyan TÜM verileri gönder
                await SendInitialFilteredData(filter);

                await Clients.Caller.SendAsync("FilterSubscribed", groupName);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("FilterSubscriptionError", ex.Message);
        }
    }

    // Tüm filtreleri temizle ve global feed'e abone ol
    public async Task UnsubscribeFromFilters()
    {
        if (_connectionFilters.TryRemove(Context.ConnectionId, out var filter))
        {
            var groupName = GetGroupNameFromFilter(filter);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        // "all" grubuna geri ekle (filtresiz kullanıcılar için)
        await Groups.AddToGroupAsync(Context.ConnectionId, "all");

        await Clients.Caller.SendAsync("FilterUnsubscribed");
    }

    // Connection kurulduğunda initial data gönder ve "all" grubuna ekle
    public override async Task OnConnectedAsync()
    {
        try
        {
            // Yeni kullanıcıyı "all" grubuna ekle (filtresiz kullanıcılar için)
            await Groups.AddToGroupAsync(Context.ConnectionId, "all");

            // ServiceProvider'dan LoomService'i al
            var serviceProvider = Context.GetHttpContext()?.RequestServices;
            if (serviceProvider != null)
            {
                using var scope = serviceProvider.CreateScope();
                var loomService = scope.ServiceProvider.GetService<ILoomService>();
                
                if (loomService != null)
                {
                    // Tüm loom verilerini filtrelerle birlikte al
                    var result = await loomService.GetLoomsWithFiltersAsync(null);
                    
                    if (result.IsSuccess && result.Data != null)
                    {
                        // Yeni bağlanan kullanıcıya initial data gönder
                        await Clients.Caller.SendAsync("FilteredLoomsDataChanged", result.Data);
                        
                        Console.WriteLine($"📡 Initial data sent to new connection: {Context.ConnectionId}");
                        Console.WriteLine($"   Looms: {result.Data.looms?.Count()}, Filters: {result.Data.filters?.Count()}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ OnConnectedAsync error: {ex.Message}");
            
            // Hata durumunda en azından boş data gönder
            await Clients.Caller.SendAsync("FilteredLoomsDataChanged", new LoomsWithFilters
            {
                looms = Enumerable.Empty<Loom>(),
                filters = Enumerable.Empty<FilterOption>()
            });
        }

        await base.OnConnectedAsync();
    }

    // Connection kapandığında temizlik yap
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionFilters.TryRemove(Context.ConnectionId, out var filter))
        {
            var groupName = GetGroupNameFromFilter(filter);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Belirli bir loom'un değişikliğini filtreli olarak gönder
    public async Task SendLoomChangeToFilteredGroups(Loom loom)
    {
        await SendLoomChangeToFilteredGroupsStatic(Clients, loom, _connectionFilters);
    }

    // Static method - SQL Dependency'den çağrılabilir
    public static async Task SendLoomChangeToFilteredGroupsStatic(IHubCallerClients clients, Loom loom, ConcurrentDictionary<string, LoomFilter> connectionFilters)
    {
        // 1. Filtresiz kullanıcılara gönder (all grubuna)
        await clients.Group("all").SendAsync("LoomCurrentlyStatusChanged", loom);

        // 2. Her aktif filtre grubu için kontrol et
        var activeGroups = connectionFilters.Values.Distinct().ToList();
        
        foreach (var filter in activeGroups)
        {
            if (LoomMatchesFilter(loom, filter))
            {
                var groupName = GetGroupNameFromFilter(filter);
                await clients.Group(groupName).SendAsync("LoomCurrentlyStatusChanged", loom);
            }
        }
    }

    // Filtreden grup adı oluştur
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

    // Loom'un filtreye uygun olup olmadığını kontrol et
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

    // Filtre seçildiğinde ilk seferde o filtreye uyan tüm verileri gönder
    private async Task SendInitialFilteredData(LoomFilter filter)
    {
        try
        {
            Console.WriteLine($"🔍 SendInitialFilteredData başladı. ConnectionId: {Context.ConnectionId}");
            
            // ServiceProvider'dan LoomService'i al
            var serviceProvider = Context.GetHttpContext()?.RequestServices;
            if (serviceProvider == null)
            {
                Console.WriteLine("❌ ServiceProvider null!");
                return;
            }

            using var scope = serviceProvider.CreateScope();
            var loomService = scope.ServiceProvider.GetService<ILoomService>();
            
            if (loomService == null)
            {
                Console.WriteLine("❌ LoomService null!");
                return;
            }

            Console.WriteLine($"🔍 Filter: {JsonSerializer.Serialize(filter)}");
            
            // Filtreli verileri al
            var result = await loomService.GetLoomsWithFiltersAsync(filter);
            
            Console.WriteLine($"🔍 Repository result: Success={result.IsSuccess}");
            
            if (result.IsSuccess && result.Data != null)
            {
                Console.WriteLine($"🔍 Data received: Looms={result.Data.looms?.Count()}, Filters={result.Data.filters?.Count()}");
                
                // Sadece bu kullanıcıya filtreye uyan TÜM verileri gönder
                await Clients.Caller.SendAsync("FilteredLoomsDataChanged", result.Data);
                
                Console.WriteLine($"✅ Filtered data sent to {Context.ConnectionId}");
            }
            else
            {
                Console.WriteLine($"❌ Repository error: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SendInitialFilteredData error: {ex.Message}");
            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
        }
    }
}