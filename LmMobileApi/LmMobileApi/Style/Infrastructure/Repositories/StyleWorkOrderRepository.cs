using Dapper;
using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Results;
using LmMobileApi.Style.Domain;
using System.Text.Json;

namespace LmMobileApi.Style.Infrastructure.Repositories;

public class StyleWorkOrderRepository : DapperRepository, IStyleWorkOrderRepository
{
    public StyleWorkOrderRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }

    public async Task<Result<IEnumerable<StyleWorkOrder>>> GetStyleWorkOrdersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = "EXEC tsp_GetStyleWorkOrdersNested_XML";
            
            var rawResult = await QueryAsync<StyleWorkOrderRaw>(sql, cancellationToken: cancellationToken);
            if (!rawResult.IsSuccess)
                return Result<IEnumerable<StyleWorkOrder>>.Failure(rawResult.Error!);
                
            var styleWorkOrders = ParseStyleWorkOrders(rawResult.Data!);
            
            return Result<IEnumerable<StyleWorkOrder>>.Success(styleWorkOrders);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<StyleWorkOrder>>.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<StyleWorkOrderFilterOption>>> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"🔍 GetFilterOptionsAsync started (legacy call)");
            
            // Get StyleWorkOrders first, then extract filter options from them
            var styleWorkOrdersResult = await GetStyleWorkOrdersAsync(cancellationToken);
            if (!styleWorkOrdersResult.IsSuccess)
            {
                Console.WriteLine($"❌ Failed to get StyleWorkOrders for filters: {styleWorkOrdersResult.Error}");
                return Result<IEnumerable<StyleWorkOrderFilterOption>>.Failure(styleWorkOrdersResult.Error!);
            }
            
            var styleWorkOrders = styleWorkOrdersResult.Data!.ToList();
            Console.WriteLine($"✅ {styleWorkOrders.Count} StyleWorkOrders found for filter generation");
            
            var filterOptions = GenerateFilterOptionsFromData(styleWorkOrders);
            
            return Result<IEnumerable<StyleWorkOrderFilterOption>>.Success(filterOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GetFilterOptionsAsync Exception: {ex.Message}");
            return Result<IEnumerable<StyleWorkOrderFilterOption>>.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<StyleWorkOrder>>> GetFilteredStyleWorkOrdersAsync(StyleWorkOrderFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = "EXEC tsp_GetStyleWorkOrdersNested_XML";
            
            var rawResult = await QueryAsync<StyleWorkOrderRaw>(sql, cancellationToken: cancellationToken);
            if (!rawResult.IsSuccess)
                return Result<IEnumerable<StyleWorkOrder>>.Failure(rawResult.Error!);
                
            var styleWorkOrders = ParseStyleWorkOrders(rawResult.Data!);
            
            // Apply filter
            if (!string.IsNullOrEmpty(filter.LoomGroupName))
            {
                styleWorkOrders = styleWorkOrders.Where(x => x.LoomGroupName == filter.LoomGroupName);
            }
            
            return Result<IEnumerable<StyleWorkOrder>>.Success(styleWorkOrders);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<StyleWorkOrder>>.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<StyleWorkOrdersWithFilters>> GetStyleWorkOrdersWithFiltersAsync(StyleWorkOrderFilter? filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"🗄️ Repository - Filter: {filter?.LoomGroupName ?? "null"}");
            
            // Get ALL data first (only one DB call)
            var allDataResult = await GetStyleWorkOrdersAsync(cancellationToken);
            
            Console.WriteLine($"🗄️ Repository - Data Success: {allDataResult.IsSuccess}");
            
            if (!allDataResult.IsSuccess)
            {
                Console.WriteLine($"❌ Data Error: {allDataResult.Error}");
                return Result<StyleWorkOrdersWithFilters>.Failure(allDataResult.Error!);
            }
            
            var allData = allDataResult.Data!.ToList();
            Console.WriteLine($"✅ Retrieved {allData.Count} StyleWorkOrders from DB");
            
            // Apply filter in memory if needed
            var filteredData = filter == null ? allData : allData.Where(x => 
                string.IsNullOrEmpty(filter.LoomGroupName) || x.LoomGroupName == filter.LoomGroupName
            ).ToList();
            
            Console.WriteLine($"🔍 After filtering: {filteredData.Count} StyleWorkOrders");
            
            // Generate filters from ALL data (not filtered data)
            var filterOptions = GenerateFilterOptionsFromData(allData);
            
            var result = new StyleWorkOrdersWithFilters
            {
                StyleWorkOrders = filteredData,
                Filters = filterOptions
            };
            
            Console.WriteLine($"✅ Repository Success - StyleWorkOrders: {filteredData.Count}, Filters: {filterOptions.Count()}");
            
            return Result<StyleWorkOrdersWithFilters>.Success(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Repository Exception: {ex.Message}");
            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            return Result<StyleWorkOrdersWithFilters>.Failure($"Database error: {ex.Message}");
        }
    }

    private IEnumerable<StyleWorkOrder> ParseStyleWorkOrders(IEnumerable<StyleWorkOrderRaw> rawResults)
    {
        var result = new List<StyleWorkOrder>();
        
        foreach (var raw in rawResults)
        {
            var styleWorkOrder = new StyleWorkOrder
            {
                LoomGroupName = raw.LoomGroupName,
                LoomNo = raw.LoomNo,
                AvgSpeed = raw.AvgSpeed,
                ProductedLength = raw.ProductedLength,
                TotalLength = raw.TotalLength,
                StyleName = raw.StyleName,
                Density = raw.Density,
                PlannedLength = raw.PlannedLength,
                SureFarki = raw.SureFarki,
                Details = new List<StyleWorkOrderDetail>()
            };
            
            // Parse JSON details
            if (!string.IsNullOrEmpty(raw.Details))
            {
                try
                {
                    var details = JsonSerializer.Deserialize<List<StyleWorkOrderDetailRaw>>(raw.Details);
                    if (details != null)
                    {
                        styleWorkOrder.Details = details.Select(d => new StyleWorkOrderDetail
                        {
                            StyleName = d.StyleName,
                            ProductedLength = d.ProductedLength,
                            WorkPriority = d.WorkPriority,
                            TotalLength = d.TotalLength,
                            StartDate = DateTime.Parse(d.StartDate),
                            EndDate = DateTime.Parse(d.EndDate)
                        }).ToList();
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing error for LoomNo {raw.LoomNo}: {ex.Message}");
                    // Continue with empty details if JSON parsing fails
                }
            }
            
            result.Add(styleWorkOrder);
        }
        
        return result;
    }

    private IEnumerable<StyleWorkOrderFilterOption> GenerateFilterOptionsFromData(List<StyleWorkOrder> allData)
    {
        Console.WriteLine($"🔍 GenerateFilterOptionsFromData started with {allData.Count} items");
        
        var filterOptions = new List<StyleWorkOrderFilterOption>();
        
        // LoomGroupName options
        var loomGroupNames = allData
            .Where(s => !string.IsNullOrEmpty(s.LoomGroupName?.Trim()))
            .Select(s => s.LoomGroupName.Trim())
            .Distinct()
            .OrderBy(x => x)
            .ToList();
            
        if (loomGroupNames.Any())
        {
            filterOptions.Add(new StyleWorkOrderFilterOption
            {
                FilterType = "LoomGroupName",
                Options = loomGroupNames
            });
            Console.WriteLine($"   🏷️ LoomGroup Names: {loomGroupNames.Count} options - {string.Join(", ", loomGroupNames.Take(3))}...");
        }
        
        Console.WriteLine($"✅ Generated {filterOptions.Count} filter options successfully");
        return filterOptions;
    }

    // Helper classes for raw data from stored procedure
    private class StyleWorkOrderRaw
    {
        public string LoomGroupName { get; set; } = string.Empty;
        public string LoomNo { get; set; } = string.Empty;
        public int AvgSpeed { get; set; }
        public double ProductedLength { get; set; }
        public double TotalLength { get; set; }
        public string StyleName { get; set; } = string.Empty;
        public double Density { get; set; }
        public double PlannedLength { get; set; }
        public string SureFarki { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty; // JSON string
    }

    private class StyleWorkOrderDetailRaw
    {
        public string StyleName { get; set; } = string.Empty;
        public double ProductedLength { get; set; }
        public int WorkPriority { get; set; }
        public double TotalLength { get; set; }
        public string StartDate { get; set; } = string.Empty; // ISO string
        public string EndDate { get; set; } = string.Empty; // ISO string
    }
} 