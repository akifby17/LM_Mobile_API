namespace LmMobileApi.Operations.Domain;

public class CurrentOperation
{
    public string OperationName { get; set; } = string.Empty;
    public string OperationGroupCode { get; set; } = string.Empty;
    public string LineDuration { get; set; } = string.Empty;
    public int LoomCount { get; set; }
    public decimal OperationPercentage { get; set; }
    public List<CurrentOperationDetail> Details { get; set; } = new();
}