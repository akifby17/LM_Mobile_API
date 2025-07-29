
using LmMobileApi.Looms.Domain;

public class LoomsWithFilters
{
    public IEnumerable<Loom> looms { get; set; } = Enumerable.Empty<Loom>();
    public IEnumerable<FilterOption> filters { get; set; } = Enumerable.Empty<FilterOption>();
}