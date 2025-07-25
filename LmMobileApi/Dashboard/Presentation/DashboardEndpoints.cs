using LmMobileApi.Dashboard.Application.Services;
using LmMobileApi.Dashboard.Domain;
using LmMobileApi.Shared.Endpoints;

namespace LmMobileApi.Dashboard.Presentation;

public class DashboardEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard/activeShiftPieChart", async (IDashboardService dashboardService, CancellationToken cancellationToken) =>
            {
                var result = await dashboardService.GetActiveShiftPieChartAsync(cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Error);
            })
            .WithName("GetActiveShiftPieChartAsync")
            .Produces<ActiveShiftPieChart>()
            .WithTags("Dashboard");
    }
}
