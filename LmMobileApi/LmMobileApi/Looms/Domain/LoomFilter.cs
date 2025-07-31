using System.Text.Json.Serialization;

namespace LmMobileApi.Looms.Domain;

public class LoomFilter
{
    [JsonPropertyName("eventNameTR")]
    public string? EventNameTR { get; set; }
    public string? ModelName { get; set; }
    public string? MarkName { get; set; }
    public string? GroupName { get; set; }
    public string? HallName { get; set; }
    public string? ClassName { get; set; }

    /// <summary>
    /// Filter'ın eşitlik kontrolü için
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not LoomFilter other) return false;

        return EventNameTR == other.EventNameTR &&
               ModelName == other.ModelName &&
               GroupName == other.GroupName &&
               HallName == other.HallName &&
               ClassName == other.ClassName &&
               MarkName == other.MarkName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EventNameTR, ModelName, GroupName, MarkName, HallName,ClassName);
    }
}