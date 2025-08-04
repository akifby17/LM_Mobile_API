using LmMobileApi.Operations.Domain;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Operations.Application.Services;

public interface ICurrentOperationService
{
    /// <summary>
    /// Mode-based endpoint: 0 = All data + filters, 1 = Filtered data + filters
    /// </summary>
    Task<Result<CurrentOperationsWithFilters>> GetCurrentOperationsByModeAsync(CurrentOperationRequest request, CancellationToken cancellationToken = default);
    
    // Backward compatibility methods (optional)
    Task<Result<IEnumerable<CurrentOperation>>> GetCurrentOperationsAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<CurrentOperationFilterOption>>> GetFilterOptionsAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<CurrentOperation>>> GetFilteredCurrentOperationsAsync(CurrentOperationFilter filter, CancellationToken cancellationToken = default);
    Task<Result<CurrentOperationsWithFilters>> GetCurrentOperationsWithFiltersAsync(CurrentOperationFilter? filter = null, CancellationToken cancellationToken = default);
}