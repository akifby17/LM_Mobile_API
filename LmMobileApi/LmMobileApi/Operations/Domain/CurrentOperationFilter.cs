using System.Text.Json.Serialization;

namespace LmMobileApi.Operations.Domain;

public class CurrentOperationFilter
{
    [JsonPropertyName("operationGroupCode")]
    public string? OperationGroupCode { get; set; }

    /// <summary>
    /// Filter'ın eşitlik kontrolü için
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not CurrentOperationFilter other) return false;
        return OperationGroupCode?.Trim()  == other.OperationGroupCode?.Trim();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(OperationGroupCode);
    }
}