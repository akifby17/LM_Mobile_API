namespace LmMobileApi.Style.Domain;

public class StyleWorkOrdersWithFilters
{
    public IEnumerable<StyleWorkOrder> StyleWorkOrders { get; set; } = Enumerable.Empty<StyleWorkOrder>();
    public IEnumerable<StyleWorkOrderFilterOption> Filters { get; set; } = Enumerable.Empty<StyleWorkOrderFilterOption>();
} 