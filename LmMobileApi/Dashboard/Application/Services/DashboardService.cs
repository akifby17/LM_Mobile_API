using LmMobileApi.Dashboard.Domain;
using LmMobileApi.Dashboard.Infrastructure.Repositories;
using LmMobileApi.Shared.Application;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Dashboard.Application.Services;

public interface IDashboardService
{
    Task<Result<ActiveShiftPieChart>> GetActiveShiftPieChartAsync(CancellationToken cancellationToken = default);
}

public class DashboardService(IDashboardRepository repository) : ApplicationService(repository), IDashboardService
{
    private IDashboardRepository DashboardRepository => Repository as IDashboardRepository
        ?? throw new InvalidOperationException("Repository is not of type IDashboardRepository");

    public Task<Result<ActiveShiftPieChart>> GetActiveShiftPieChartAsync(CancellationToken cancellationToken = default)
    {
        return DashboardRepository.GetActiveShiftPieChartAsync(cancellationToken);
    }
}
