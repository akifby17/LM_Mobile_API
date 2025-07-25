namespace LmMobileApi.Dashboard.Domain;

public class ActiveShiftPieChart
{
    public double Efficiency { get; private set; }
    public double WeftStop { get; private set; }
    public double WarpStop { get; private set; }
    public double OtherStop { get; private set; }
    public double OperationStop { get; private set; }
    public double PickCounter { get; private set; }
    public double ProductedLength { get; private set; }

    private ActiveShiftPieChart() { }
}
