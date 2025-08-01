namespace LmMobileApi.Style.Domain;

public class StyleWorkOrderFilterOption
{
    public string FilterType { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
} 