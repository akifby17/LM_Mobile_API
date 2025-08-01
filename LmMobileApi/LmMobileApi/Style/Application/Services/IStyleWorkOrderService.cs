using LmMobileApi.Style.Domain;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Style.Application.Services;

public interface IStyleWorkOrderService
{
    /// <summary>
    /// Mode-based endpoint: 0 = All data + filters, 1 = Filtered data + filters
    /// </summary>
    Task<Result<StyleWorkOrdersWithFilters>> GetStyleWorkOrdersByModeAsync(StyleWorkOrderRequest request, CancellationToken cancellationToken = default);
    
    // Backward compatibility methods (optional)
    Task<Result<IEnumerable<StyleWorkOrder>>> GetStyleWorkOrdersAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<StyleWorkOrderFilterOption>>> GetFilterOptionsAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<StyleWorkOrder>>> GetFilteredStyleWorkOrdersAsync(StyleWorkOrderFilter filter, CancellationToken cancellationToken = default);
    Task<Result<StyleWorkOrdersWithFilters>> GetStyleWorkOrdersWithFiltersAsync(StyleWorkOrderFilter? filter = null, CancellationToken cancellationToken = default);
} 