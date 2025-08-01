using LmMobileApi.Shared.Data;
using LmMobileApi.Style.Domain;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Style.Infrastructure.Repositories;

public interface IStyleWorkOrderRepository : IRepository
{
    Task<Result<IEnumerable<StyleWorkOrder>>> GetStyleWorkOrdersAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<StyleWorkOrderFilterOption>>> GetFilterOptionsAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<StyleWorkOrder>>> GetFilteredStyleWorkOrdersAsync(StyleWorkOrderFilter filter, CancellationToken cancellationToken = default);
    Task<Result<StyleWorkOrdersWithFilters>> GetStyleWorkOrdersWithFiltersAsync(StyleWorkOrderFilter? filter = null, CancellationToken cancellationToken = default);
} 