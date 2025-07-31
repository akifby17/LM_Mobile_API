namespace LmMobileApi.Looms.Domain;

public class Loom : IEquatable<Loom>
{
    public string LoomNo { get; set; } = string.Empty;
    public double Efficiency { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string WeaverName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public int LoomSpeed { get; set; }
    public string HallName { get; set; } = string.Empty;
    public string MarkName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string WarpName { get; set; } = string.Empty;
    public string VariantNo { get; set; } = string.Empty;
    public string StyleName { get; set; } = string.Empty;
    public double WeaverEff { get; set; }
    public string EventDuration { get; set; } = string.Empty;
    public double ProductedLength { get; set; }
    public double TotalLength { get; set; }
    public string EventNameTR { get; set; } = string.Empty;
    public string OpDuration { get; set; } = string.Empty;

    private Loom() { }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is Loom other && Equals(other);
    }

    public bool Equals(Loom? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return LoomNo == other.LoomNo &&
               Math.Abs(Efficiency - other.Efficiency) < 0.0001 &&
               OperationName == other.OperationName &&
               OperatorName == other.OperatorName &&
               WeaverName == other.WeaverName &&
               EventId == other.EventId &&
               LoomSpeed == other.LoomSpeed &&
               HallName == other.HallName &&
               MarkName == other.MarkName &&
               ModelName == other.ModelName &&
               GroupName == other.GroupName &&
               ClassName == other.ClassName &&
               WarpName == other.WarpName &&
               VariantNo == other.VariantNo &&
               StyleName == other.StyleName &&
               Math.Abs(WeaverEff - other.WeaverEff) < 0.0001 &&
               EventDuration == other.EventDuration &&
               Math.Abs(ProductedLength - other.ProductedLength) < 0.0001 &&
               Math.Abs(TotalLength - other.TotalLength) < 0.0001 &&
               EventNameTR == other.EventNameTR &&
               OpDuration == other.OpDuration;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(LoomNo);
        hash.Add(Efficiency);
        hash.Add(OperationName);
        hash.Add(OperatorName);
        hash.Add(WeaverName);
        hash.Add(EventId);
        hash.Add(LoomSpeed);
        hash.Add(HallName);
        hash.Add(MarkName);
        hash.Add(ModelName);
        hash.Add(GroupName);
        hash.Add(ClassName);
        hash.Add(WarpName);
        hash.Add(VariantNo);
        hash.Add(StyleName);
        hash.Add(WeaverEff);
        hash.Add(EventDuration);
        hash.Add(ProductedLength);
        hash.Add(TotalLength);
        hash.Add(EventNameTR);
        hash.Add(OpDuration);
        return hash.ToHashCode();
    }

    public static bool operator ==(Loom? left, Loom? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Loom? left, Loom? right)
    {
        return !(left == right);
    }
}
