using LmMobileApi.Shared.Data;
using LmMobileApi.Operations.Domain;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Operations.Infrastructure.Repositories;

public interface ICurrentOperationRepository : IRepository
{
    Task<Result<IEnumerable<CurrentOperation>>> GetCurrentOperationsAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<CurrentOperationFilterOption>>> GetFilterOptionsAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<CurrentOperation>>> GetFilteredCurrentOperationsAsync(CurrentOperationFilter filter, CancellationToken cancellationToken = default);
    Task<Result<CurrentOperationsWithFilters>> GetCurrentOperationsWithFiltersAsync(CurrentOperationFilter? filter = null, CancellationToken cancellationToken = default);
}