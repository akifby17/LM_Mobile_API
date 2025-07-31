namespace LmMobileApi.Looms.Domain;

public class FilterOption
{
    public string Key { get; set; } = null!;
    public IEnumerable<string> Values { get; set; } = Enumerable.Empty<string>();
}