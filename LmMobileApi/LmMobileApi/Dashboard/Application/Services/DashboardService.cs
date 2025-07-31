using LmMobileApi.Dashboard.Domain;
using LmMobileApi.Dashboard.Infrastructure.Repositories;
using LmMobileApi.Shared.Application;
using LmMobileApi.Shared.Results;
using System.Reflection.Metadata.Ecma335;

namespace LmMobileApi.Dashboard.Application.Services;

public interface IDashboardService
{
    Task<Result<ActiveShiftPieChart>> GetActiveShiftPieChartAsync(CancellationToken cancellationToken = default);
    public Task<Result<ActiveShiftPieChart>> GetPastDatePieChartAsync(
    PastDateMode mode,
    string? customStartDate,
    string? customEndDate,
    CancellationToken cancellationToken = default);

    // Yeni dual data metodu
    Task<Result<DashboardDualResponse>> GetDashboardDualDataAsync(
        PastDateMode mode,
        string? customStartDate,
        string? customEndDate,
        CancellationToken cancellationToken = default);
}

public class DashboardService(IDashboardRepository repository) : ApplicationService(repository), IDashboardService
{
    private IDashboardRepository DashboardRepository => Repository as IDashboardRepository
        ?? throw new InvalidOperationException("Repository is not of type IDashboardRepository");

    public Task<Result<ActiveShiftPieChart>> GetActiveShiftPieChartAsync(CancellationToken cancellationToken = default)
    {
        return DashboardRepository.GetActiveShiftPieChartAsync(cancellationToken);
    }

    public Task<Result<ActiveShiftPieChart>> GetPastDatePieChartAsync(
     PastDateMode mode,
     string? customStartDate,
     string? customEndDate,
     CancellationToken cancellationToken = default)
    {
        // Doğrudan repository’e yeni SP parametrelerini ilet
        return DashboardRepository.GetPastDatePieChartAsync(
            mode,
            customStartDate,
            customEndDate,
            cancellationToken);
    }

    public async Task<Result<DashboardDualResponse>> GetDashboardDualDataAsync(
        PastDateMode mode,
        string? customStartDate,
        string? customEndDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Charts verisini al
            var chartsResult = await DashboardRepository.GetPastDatePieChartAsync(
                mode, customStartDate, customEndDate, cancellationToken);
            
            if (!chartsResult.IsSuccess)
                return Result<DashboardDualResponse>.Failure(chartsResult.Error);

            // Operations verisini al - Aynı parametrelerle
            var operationsResult = await DashboardRepository.GetOperationsDataAsync(
                mode, customStartDate, customEndDate, cancellationToken);
            
            if (!operationsResult.IsSuccess)
                return Result<DashboardDualResponse>.Failure(operationsResult.Error);

            // Dual response oluştur
            var dualResponse = new DashboardDualResponse
            {
                Charts = new List<ActiveShiftPieChart> { chartsResult.Data! },
                Operations = operationsResult.Data!
            };

            return Result<DashboardDualResponse>.Success(dualResponse);
        }
        catch (Exception ex)
        {
            return Result<DashboardDualResponse>.Failure($"Dashboard dual data error: {ex.Message}");
        }
    }

}
