using LmMobileApi.Looms.Domain;
using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Looms.Infrastructure.Repositories;

public interface ILoomRepository : IRepository
{
    Task<Result<IEnumerable<Loom>>> GetLoomsCurrentlyStatusAsync(CancellationToken cancellationToken = default);
    Task<Result<Loom>> GetLoomCurrentlyStatusAsync(string loomNo, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<Loom>>> GetFilteredLoomsAsync(LoomFilter filter, CancellationToken cancellationToken = default);
    Task<Result<LoomsWithFilters>> GetLoomsWithFiltersAsync(LoomFilter? filter = null, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<FilterOption>>> GetFilterOptionsAsync(CancellationToken cancellationToken = default);
}

public class LoomRepository(IUnitOfWork unitOfWork) : DapperRepository(unitOfWork), ILoomRepository
{
    public Task<Result<IEnumerable<Loom>>> GetLoomsCurrentlyStatusAsync(CancellationToken cancellationToken = default)
    {
        const string query = @"SELECT * FROM tvw_mobile_Looms_CurrentlyStatus ORDER BY LoomNo";
        return QueryAsync<Loom>(query, cancellationToken: cancellationToken);
    }

    public async Task<Result<Loom>> GetLoomCurrentlyStatusAsync(string loomNo, CancellationToken cancellationToken = default)
    {
        const string query = @"SELECT * FROM tvw_mobile_Looms_CurrentlyStatus WHERE LoomNo = @LoomNo";
        var loom = await QueryFirstOrDefaultAsync<Loom>(query, new { LoomNo = loomNo }, cancellationToken: cancellationToken);
        if (loom.Data is null)
            return Error.NotFound;
        return loom!;
    }

    public Task<Result<IEnumerable<Loom>>> GetFilteredLoomsAsync(LoomFilter filter, CancellationToken cancellationToken = default)
    {
        var query = @"SELECT * FROM tvw_mobile_Looms_CurrentlyStatus WHERE 1=1";
        var parameters = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(filter.HallName))
        {
            query += " AND HallName = @HallName";
            parameters.Add("HallName", filter.HallName);
        }

        if (!string.IsNullOrEmpty(filter.MarkName))
        {
            query += " AND MarkName = @MarkName";
            parameters.Add("MarkName", filter.MarkName);
        }

        if (!string.IsNullOrEmpty(filter.GroupName))
        {
            query += " AND GroupName = @GroupName";
            parameters.Add("GroupName", filter.GroupName);
        }

        if (!string.IsNullOrEmpty(filter.ModelName))
        {
            query += " AND ModelName = @ModelName";
            parameters.Add("ModelName", filter.ModelName);
        }

        if (!string.IsNullOrEmpty(filter.ClassName))
        {
            query += " AND ClassName = @ClassName";
            parameters.Add("ClassName", filter.ClassName);
        }

        if (!string.IsNullOrEmpty(filter.EventNameTR))
        {
            query += " AND EventNameTR = @EventNameTR";
            parameters.Add("EventNameTR", filter.EventNameTR);
        }

        query += " ORDER BY LoomNo";

        return QueryAsync<Loom>(query, parameters, cancellationToken);
    }

    public async Task<Result<LoomsWithFilters>> GetLoomsWithFiltersAsync(LoomFilter? filter = null, CancellationToken cancellationToken = default)
    {
        // Filtreli loom'ları al
        Result<IEnumerable<Loom>> loomsResult;
        if (filter != null)
        {
            loomsResult = await GetFilteredLoomsAsync(filter, cancellationToken);
        }
        else
        {
            loomsResult = await GetLoomsCurrentlyStatusAsync(cancellationToken);
        }

        if (loomsResult.IsFailure)
            return Result<LoomsWithFilters>.Failure(loomsResult.Error);

        // Zaten aldığımız loom'lardan filtre seçeneklerini oluştur
        var filterOptions = GenerateFilterOptionsFromLooms(loomsResult.Data!);

        var result = new LoomsWithFilters
        {
            looms = loomsResult.Data!,
            filters = filterOptions
        };

        return Result<LoomsWithFilters>.Success(result);
    }

    public async Task<Result<IEnumerable<FilterOption>>> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        var filterOptions = new List<FilterOption>();

        // Mevcut loom verilerinden dinamik filtre seçenekleri oluştur
        var loomsResult = await GetLoomsCurrentlyStatusAsync(cancellationToken);
        if (loomsResult.IsSuccess && loomsResult.Data?.Any() == true)
        {
            var looms = loomsResult.Data.ToList();
            Console.WriteLine($"✅ {looms.Count} loom found for filter generation");

            // Hall Names
            var hallNames = looms.Where(l => !string.IsNullOrEmpty(l.HallName.Trim()))
                                .Select(l => l.HallName.Trim()).Distinct().OrderBy(x => x).ToList();
            if (hallNames.Any())
            {
                filterOptions.Add(new FilterOption
                {
                    Key = "hallName",
                    Values = hallNames
                });
                Console.WriteLine($"   📍 Hall Names: {hallNames.Count} options");
            }

            // Mark Names
            var markNames = looms.Where(l => !string.IsNullOrEmpty(l.MarkName.Trim()))
                                .Select(l => l.MarkName.Trim()).Distinct().OrderBy(x => x).ToList();
            if (markNames.Any())
            {
                filterOptions.Add(new FilterOption  
                {
                    Key = "markName", 
                    Values = markNames
                });
                Console.WriteLine($"   🏭 Mark Names: {markNames.Count} options");
            }

            // Group Names
            var groupNames = looms.Where(l => !string.IsNullOrEmpty(l.GroupName.Trim()))
                                 .Select(l => l.GroupName.Trim()).Distinct().OrderBy(x => x).ToList();
            if (groupNames.Any())
            {
                filterOptions.Add(new FilterOption
                {
                    Key = "groupName",
                    Values = groupNames
                });
                Console.WriteLine($"   📂 Group Names: {groupNames.Count} options");
            }

            // Model Names  
            var modelNames = looms.Where(l => !string.IsNullOrEmpty(l.ModelName.Trim()))
                                 .Select(l => l.ModelName.Trim()).Distinct().OrderBy(x => x).ToList();
            if (modelNames.Any())
            {
                filterOptions.Add(new FilterOption
                {
                    Key = "modelName",
                    Values = modelNames
                });
                Console.WriteLine($"   🔧 Model Names: {modelNames.Count} options");
            }

            // Class Names
            var classNames = looms.Where(l => !string.IsNullOrEmpty(l.ClassName.Trim()))
                                 .Select(l => l.ClassName.Trim()).Distinct().OrderBy(x => x).ToList();
            if (classNames.Any())
            {
                filterOptions.Add(new FilterOption
                {
                    Key = "className",
                    Values = classNames
                });
                Console.WriteLine($"   📋 Class Names: {classNames.Count} options");
            }

            // Event Names
            var eventNames = looms.Where(l => !string.IsNullOrEmpty(l.EventNameTR.Trim()))
                                 .Select(l => l.EventNameTR.Trim()).Distinct().OrderBy(x => x).ToList();
            if (eventNames.Any())
            {
                filterOptions.Add(new FilterOption
                {
                    Key = "eventNameTR",
                    Values = eventNames
                });
                Console.WriteLine($"   🚦 Event Names: {eventNames.Count} options");
            }

            Console.WriteLine($"✅ Generated {filterOptions.Count} filter options from existing data");
        }
        else
        {
            Console.WriteLine("❌ No looms found - cannot generate filter options");
        }

        return Result<IEnumerable<FilterOption>>.Success(filterOptions);
    }

    private static List<FilterOption> GenerateFilterOptionsFromLooms(IEnumerable<Loom> looms)
    {
        var filterOptions = new List<FilterOption>();
        var loomsList = looms.ToList();
        
        Console.WriteLine($"🔄 Generating filters from {loomsList.Count} looms");

        // Hall Names
        var hallNames = loomsList.Where(l => !string.IsNullOrEmpty(l.HallName))
                                .Select(l => l.HallName).Distinct().OrderBy(x => x).ToList();
        if (hallNames.Any())
        {
            filterOptions.Add(new FilterOption
            {
                Key = "hallName",
                Values = hallNames
            });
            Console.WriteLine($"   📍 Hall Names: {hallNames.Count} options - {string.Join(", ", hallNames.Take(3))}...");
        }

        // Mark Names
        var markNames = loomsList.Where(l => !string.IsNullOrEmpty(l.MarkName))
                                .Select(l => l.MarkName).Distinct().OrderBy(x => x).ToList();
        if (markNames.Any())
        {
            filterOptions.Add(new FilterOption
            {
                Key = "markName", 
                Values = markNames
            });
            Console.WriteLine($"   🏭 Mark Names: {markNames.Count} options - {string.Join(", ", markNames.Take(3))}...");
        }

        // Group Names
        var groupNames = loomsList.Where(l => !string.IsNullOrEmpty(l.GroupName))
                                 .Select(l => l.GroupName).Distinct().OrderBy(x => x).ToList();
        if (groupNames.Any())
        {
            filterOptions.Add(new FilterOption
            {
                Key = "groupName",
                Values = groupNames
            });
            Console.WriteLine($"   📂 Group Names: {groupNames.Count} options - {string.Join(", ", groupNames.Take(3))}...");
        }

        // Model Names  
        var modelNames = loomsList.Where(l => !string.IsNullOrEmpty(l.ModelName))
                                 .Select(l => l.ModelName).Distinct().OrderBy(x => x).ToList();
        if (modelNames.Any())
        {
            filterOptions.Add(new FilterOption
            {
                Key = "modelName",
                Values = modelNames
            });
            Console.WriteLine($"   🔧 Model Names: {modelNames.Count} options - {string.Join(", ", modelNames.Take(3))}...");
        }

        // Class Names
        var classNames = loomsList.Where(l => !string.IsNullOrEmpty(l.ClassName))
                                 .Select(l => l.ClassName).Distinct().OrderBy(x => x).ToList();
        if (classNames.Any())
        {
            filterOptions.Add(new FilterOption
            {
                Key = "className",
                Values = classNames
            });
            Console.WriteLine($"   📋 Class Names: {classNames.Count} options - {string.Join(", ", classNames.Take(3))}...");
        }

        // Event Names
        var eventNames = loomsList.Where(l => !string.IsNullOrEmpty(l.EventNameTR))
                                 .Select(l => l.EventNameTR).Distinct().OrderBy(x => x).ToList();
        if (eventNames.Any())
        {
            filterOptions.Add(new FilterOption
            {
                Key = "eventNameTR",
                Values = eventNames
            });
            Console.WriteLine($"   🚦 Event Names: {eventNames.Count} options - {string.Join(", ", eventNames.Take(3))}...");
        }

        Console.WriteLine($"✅ Generated {filterOptions.Count} filter options successfully");
        
        return filterOptions;
    }
}