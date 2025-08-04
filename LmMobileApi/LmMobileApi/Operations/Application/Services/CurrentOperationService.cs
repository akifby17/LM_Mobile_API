using LmMobileApi.Operations.Domain;
using LmMobileApi.Operations.Infrastructure.Repositories;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Operations.Application.Services;

public class CurrentOperationService : ICurrentOperationService
{
    private readonly ICurrentOperationRepository _repository;

    public CurrentOperationService(ICurrentOperationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CurrentOperationsWithFilters>> GetCurrentOperationsByModeAsync(CurrentOperationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"🔧 CurrentOperationService - Mode: {request.Mode}, Filter: {request.Filter?.OperationGroupCode ?? "null"}");
            
            var result = request.Mode switch
            {
                0 => await _repository.GetCurrentOperationsWithFiltersAsync(null, cancellationToken), // Tüm veriler + filtreler
                1 => await _repository.GetCurrentOperationsWithFiltersAsync(request.Filter, cancellationToken), // Filtrelenmiş veriler + filtreler
                _ => Result<CurrentOperationsWithFilters>.Failure("Invalid mode. Use 0 for all data or 1 for filtered data.")
            };
            
            Console.WriteLine($"🔧 CurrentOperationService Result - Success: {result.IsSuccess}, Error: {(result.Error != null ? result.Error.ToString() : "none")}");
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CurrentOperationService Exception: {ex.Message}");
            return Result<CurrentOperationsWithFilters>.Failure($"Error processing request: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CurrentOperation>>> GetCurrentOperationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetCurrentOperationsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CurrentOperation>>.Failure($"Error getting current operations: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CurrentOperationFilterOption>>> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetFilterOptionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CurrentOperationFilterOption>>.Failure($"Error getting filter options: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CurrentOperation>>> GetFilteredCurrentOperationsAsync(CurrentOperationFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetFilteredCurrentOperationsAsync(filter, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CurrentOperation>>.Failure($"Error getting filtered current operations: {ex.Message}");
        }
    }

    public async Task<Result<CurrentOperationsWithFilters>> GetCurrentOperationsWithFiltersAsync(CurrentOperationFilter? filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetCurrentOperationsWithFiltersAsync(filter, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<CurrentOperationsWithFilters>.Failure($"Error getting current operations with filters: {ex.Message}");
        }
    }
}