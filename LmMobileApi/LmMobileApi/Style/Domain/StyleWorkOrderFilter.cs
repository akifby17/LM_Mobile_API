using System.Text.Json.Serialization;

namespace LmMobileApi.Style.Domain;

public class StyleWorkOrderFilter
{
    [JsonPropertyName("loomGroupName")]
    public string? LoomGroupName { get; set; }

    /// <summary>
    /// Filter'ın eşitlik kontrolü için
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not StyleWorkOrderFilter other) return false;
        return LoomGroupName == other.LoomGroupName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LoomGroupName);
    }
} 