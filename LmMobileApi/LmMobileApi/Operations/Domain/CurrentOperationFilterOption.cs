namespace LmMobileApi.Operations.Domain;

public class CurrentOperationFilterOption
{
    public string FilterType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
}