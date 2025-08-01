namespace LmMobileApi.Style.Domain;

public class StyleWorkOrder
{
    public string LoomGroupName { get; set; } = string.Empty;
    public string LoomNo { get; set; } = string.Empty;
    public int AvgSpeed { get; set; }
    public double ProductedLength { get; set; }
    public double TotalLength { get; set; }
    public string StyleName { get; set; } = string.Empty;
    public double Density { get; set; }
    public double PlannedLength { get; set; }
    public string SureFarki { get; set; } = string.Empty;
    public List<StyleWorkOrderDetail> Details { get; set; } = new();
} 