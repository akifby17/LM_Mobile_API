namespace LmMobileApi.Dashboard.Domain;

public class ActiveShiftPieChart
{
    public double Efficiency { get; set; }
    public double WeftStop { get; set; }
    public double WarpStop { get; set; }
    public double OtherStop { get; set; }
    public double OperationStop { get; set; }
    public double PickCounter { get; set; }
    public double ProductedLength { get; set; }
    public double WarpKa { get; set; }
    public double WeftKa { get; set; }
    public double LoomCount { get; set; }
    public double AvgSpeed { get; set; }
    public double WeaverEff {  get; set; }
    public double AvgDensity { get; set; }
    public ActiveShiftPieChart() { }
}

// Dual response model - charts + operations
public class DashboardDualResponse
{
    public List<ActiveShiftPieChart> Charts { get; set; } = new();
    public List<Dictionary<string, object>> Operations { get; set; } = new();
}

