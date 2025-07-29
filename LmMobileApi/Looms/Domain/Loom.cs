namespace LmMobileApi.Looms.Domain;

public class Loom : IEquatable<Loom>
{
    public string LoomNo { get; private set; } = string.Empty;
    public double Efficiency { get; private set; }
    public string OperationName { get; private set; } = string.Empty;
    public string OperatorName { get; private set; } = string.Empty;
    public string WeaverName { get; private set; } = string.Empty;
    public int EventId { get; private set; }
    public int LoomSpeed { get; private set; }
    public String HallName { get; private set; } = string.Empty;
    public String MarkName { get; private set; } = string.Empty;
    public String ModelName { get; private set; } = string.Empty;
    public String GroupName { get; private set; } = string.Empty;
    public String ClassName { get; private set; } = string.Empty;
    public String WarpName { get; private set; } = string.Empty;
    public String VariantNo { get; private set; } = string.Empty;
    public String StyleName { get; private set; } = string.Empty;
    public double WeaverEff { get; private set; }
    public String EventDuration { get; private set; }
    public double ProductedLength { get; private set; }
    public double TotalLength { get; private set; }
    public String EventNameTR { get; private set; }
    public String OpDuration { get; private set; }

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
               LoomSpeed == other.LoomSpeed;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            LoomNo,
            Efficiency,
            OperationName,
            OperatorName,
            WeaverName,
            EventId,
            LoomSpeed);
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
