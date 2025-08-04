using Dapper;
using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Results;
using LmMobileApi.Operations.Domain;
using System.Text.Json;

namespace LmMobileApi.Operations.Infrastructure.Repositories;

public class CurrentOperationRepository : DapperRepository, ICurrentOperationRepository
{
    public CurrentOperationRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }

    public async Task<Result<IEnumerable<CurrentOperation>>> GetCurrentOperationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = "EXEC tsp_GetCurrentOperationsWithDetails_XML";
            
            var rawResult = await QueryAsync<CurrentOperationRaw>(sql, cancellationToken: cancellationToken);
            if (!rawResult.IsSuccess)
                return Result<IEnumerable<CurrentOperation>>.Failure(rawResult.Error!);
                
            var currentOperations = ParseCurrentOperations(rawResult.Data!);
            
            return Result<IEnumerable<CurrentOperation>>.Success(currentOperations);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CurrentOperation>>.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CurrentOperationFilterOption>>> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"🔍 GetFilterOptionsAsync started (legacy call)");
            
            // Get CurrentOperations first, then extract filter options from them
            var currentOperationsResult = await GetCurrentOperationsAsync(cancellationToken);
            if (!currentOperationsResult.IsSuccess)
            {
                Console.WriteLine($"❌ Failed to get CurrentOperations for filters: {currentOperationsResult.Error}");
                return Result<IEnumerable<CurrentOperationFilterOption>>.Failure(currentOperationsResult.Error!);
            }
            
            var currentOperations = currentOperationsResult.Data!.ToList();
            Console.WriteLine($"✅ {currentOperations.Count} CurrentOperations found for filter generation");
            
            var filterOptions = GenerateFilterOptionsFromData(currentOperations, null);
            
            // Legacy method returns IEnumerable, so wrap single filter in array
            return Result<IEnumerable<CurrentOperationFilterOption>>.Success([filterOptions]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GetFilterOptionsAsync Exception: {ex.Message}");
            return Result<IEnumerable<CurrentOperationFilterOption>>.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CurrentOperation>>> GetFilteredCurrentOperationsAsync(CurrentOperationFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = "EXEC tsp_GetCurrentOperationsWithDetails_XML";
            
            var rawResult = await QueryAsync<CurrentOperationRaw>(sql, cancellationToken: cancellationToken);
            if (!rawResult.IsSuccess)
                return Result<IEnumerable<CurrentOperation>>.Failure(rawResult.Error!);
                
            var currentOperations = ParseCurrentOperations(rawResult.Data!);
            
            // Apply filter
            if (!string.IsNullOrEmpty(filter.OperationGroupCode?.Trim()))
            {
                currentOperations = currentOperations.Where(x => x.OperationGroupCode.Trim().Equals( filter.OperationGroupCode.Trim()));
            }
            
            return Result<IEnumerable<CurrentOperation>>.Success(currentOperations);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CurrentOperation>>.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<CurrentOperationsWithFilters>> GetCurrentOperationsWithFiltersAsync(CurrentOperationFilter? filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"🗄️ Repository - Filter: {filter?.OperationGroupCode ?? "null"}");
            
            // Get ALL data first (only one DB call) - Style pattern applied
            var allDataResult = await GetCurrentOperationsAsync(cancellationToken);
            
            Console.WriteLine($"🗄️ Repository - Data Success: {allDataResult.IsSuccess}");
            
            if (!allDataResult.IsSuccess)
            {
                Console.WriteLine($"❌ Data Error: {allDataResult.Error}");
                return Result<CurrentOperationsWithFilters>.Failure(allDataResult.Error!);
            }
            
            var allData = allDataResult.Data!.ToList();
            Console.WriteLine($"✅ Retrieved {allData.Count} CurrentOperations from DB");
            
            // Apply filter in memory if needed
            var filteredData = filter == null ? allData : allData.Where(x => 
                string.IsNullOrEmpty(filter.OperationGroupCode) || x.OperationGroupCode == filter.OperationGroupCode
            ).ToList();
            
            Console.WriteLine($"🔍 After filtering: {filteredData.Count} CurrentOperations");
            
            // Generate single filter object from ALL data (not filtered data)
            var filterOptions = GenerateFilterOptionsFromData(allData, filter);
            
            var result = new CurrentOperationsWithFilters
            {
                CurrentOperations = filteredData,
                Filters = filterOptions
            };
            
            Console.WriteLine($"✅ Repository Success - CurrentOperations: {filteredData.Count}, Filter: {filterOptions.FilterType}");
            
            return Result<CurrentOperationsWithFilters>.Success(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Repository Exception: {ex.Message}");
            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            return Result<CurrentOperationsWithFilters>.Failure($"Database error: {ex.Message}");
        }
    }

    private IEnumerable<CurrentOperation> ParseCurrentOperations(IEnumerable<CurrentOperationRaw> rawResults)
    {
        var result = new List<CurrentOperation>();
        
        foreach (var raw in rawResults)
        {
            var currentOperation = new CurrentOperation
            {
                OperationName = raw.OperationName,
                OperationGroupCode = raw.OperationGroupCode,
                LineDuration = raw.LineDuration,
                LoomCount = raw.LoomCount,
                OperationPercentage = raw.OperationPercentage,
                Details = new List<CurrentOperationDetail>()
            };
            
            // Parse JSON details
            if (!string.IsNullOrEmpty(raw.Details))
            {
                try
                {
                    var details = JsonSerializer.Deserialize<List<CurrentOperationDetailRaw>>(raw.Details);
                    if (details != null)
                    {
                        currentOperation.Details = details.Select(d => new CurrentOperationDetail
                        {
                            LoomNo = d.LoomNo,
                            OperationName = d.OperationName,
                            PersonnelName = d.PersonnelName,
                            LineDuration = d.LineDuration
                        }).ToList();
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing error for Operation {raw.OperationName}: {ex.Message}");
                    // Continue with empty details if JSON parsing fails
                }
            }
            
            result.Add(currentOperation);
        }
        
        return result;
    }

    private CurrentOperationFilterOption GenerateFilterOptionsFromData(List<CurrentOperation> allData, CurrentOperationFilter? appliedFilter)
    {
        Console.WriteLine($"🔍 GenerateFilterOptionsFromData started with {allData.Count} items");
        
        // OperationGroupCode options
        var operationGroupCodes = allData
            .Where(s => !string.IsNullOrEmpty(s.OperationGroupCode?.Trim()))
            .Select(s => s.OperationGroupCode.Trim())
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        // Add "Tümü" option at the beginning
        var allOptions = new List<string> { "Tümü" };
        allOptions.AddRange(operationGroupCodes);

        var filterOption = new CurrentOperationFilterOption
        {
            FilterType = "OperationGroupCode",
            Value = "Operasyon Grup Kodu", // Filter type description in Turkish
            Options = allOptions
        };

        Console.WriteLine($"   🏷️ Operation Group Codes: {operationGroupCodes.Count} options - {string.Join(", ", operationGroupCodes.Take(3))}...");
        Console.WriteLine($"✅ Generated filter option successfully with value: '{filterOption.Value}'");
        
        return filterOption;
    }

    // Helper classes for raw data from stored procedure
    private class CurrentOperationRaw
    {
        public string OperationName { get; set; } = string.Empty;
        public string OperationGroupCode { get; set; } = string.Empty;
        public string LineDuration { get; set; } = string.Empty;
        public int LoomCount { get; set; }
        public decimal OperationPercentage { get; set; }
        public string Details { get; set; } = string.Empty; // JSON string
    }

    private class CurrentOperationDetailRaw
    {
        public string LoomNo { get; set; } = string.Empty;
        public string OperationName { get; set; } = string.Empty;
        public string PersonnelName { get; set; } = string.Empty;
        public string LineDuration { get; set; } = string.Empty;
    }
}