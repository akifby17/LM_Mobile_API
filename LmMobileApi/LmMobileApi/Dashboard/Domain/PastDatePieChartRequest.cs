public enum PastDateMode
{
    Active,
    Day,
    Week,
    Month,
    Custom
}

public record PastDatePieChartRequest(
    PastDateMode Mode,
    string? CustomStartDate,
    string? CustomEndDate
);
