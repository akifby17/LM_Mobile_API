using LmMobileApi.Dashboard.Domain;
using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Dashboard.Infrastructure.Repositories;

public interface IDashboardRepository : IRepository
{
    Task<Result<ActiveShiftPieChart>> GetActiveShiftPieChartAsync(CancellationToken cancellationToken = default);
}

public class DashboardRepository(IUnitOfWork unitOfWork) : DapperRepository(unitOfWork), IDashboardRepository
{
    public async Task<Result<ActiveShiftPieChart>> GetActiveShiftPieChartAsync(CancellationToken cancellationToken = default)
    {
        const string query = @"EXEC dbo.tsp_GetActiveShiftPieChart";
        var activeShiftPieChart = await QueryFirstOrDefaultAsync<ActiveShiftPieChart>(query, null, cancellationToken: cancellationToken);
        if (activeShiftPieChart.Data is null)
            return Error.NotFound;
        return activeShiftPieChart!;
    }
}
