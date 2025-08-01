namespace LmMobileApi.Style.Domain;

public class StyleWorkOrderDetail
{
    public string StyleName { get; set; } = string.Empty;
    public double ProductedLength { get; set; }
    public int WorkPriority { get; set; }
    public double TotalLength { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
} 