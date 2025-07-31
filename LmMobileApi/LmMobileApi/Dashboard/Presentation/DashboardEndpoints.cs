using LmMobileApi.Dashboard.Application.Services;
using LmMobileApi.Dashboard.Domain;
using LmMobileApi.Dashboard.Infrastructure.Repositories;
using LmMobileApi.Shared.Endpoints;

namespace LmMobileApi.Dashboard.Presentation;

public class DashboardEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
      

     

        // Yeni dual endpoint - charts + operations (Ayrı scope'lar ile)
        app.MapPost("/api/dashboard/getDualData",
            async (PastDatePieChartRequest req,
                   IServiceProvider serviceProvider,
                   CancellationToken ct) =>
            {
                try
                {
                    ActiveShiftPieChart? chartsData = null;
                    List<Dictionary<string, object>>? operationsData = null;

                    // Charts verisini al - İlk scope
                    using (var scope1 = serviceProvider.CreateScope())
                    {
                        var dashboardService1 = scope1.ServiceProvider.GetRequiredService<IDashboardService>();
                        var chartsResult = await dashboardService1.GetPastDatePieChartAsync(
                            req.Mode, req.CustomStartDate, req.CustomEndDate, ct);
                        
                        if (!chartsResult.IsSuccess)
                            return Results.BadRequest(chartsResult.Error);
                        
                        chartsData = chartsResult.Data;
                    }

                    // Operations verisini al - İkinci scope (aynı parametrelerle)
                    using (var scope2 = serviceProvider.CreateScope())
                    {
                        var dashboardService2 = scope2.ServiceProvider.GetRequiredService<IDashboardService>();
                        var repository = scope2.ServiceProvider.GetRequiredService<IDashboardRepository>();
                        var operationsResult = await repository.GetOperationsDataAsync(
                            req.Mode, req.CustomStartDate, req.CustomEndDate, ct);
                        
                        if (!operationsResult.IsSuccess)
                            return Results.BadRequest(operationsResult.Error);
                        
                        operationsData = operationsResult.Data;
                    }

                    // Dual response oluştur
                    var dualResponse = new DashboardDualResponse
                    {
                        Charts = new List<ActiveShiftPieChart> { chartsData! },
                        Operations = operationsData!
                    };

                    return Results.Ok(dualResponse);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { code = $"Dual data error: {ex.Message}", description = "" });
                }
            })
            .WithName("GetDashboardDualData")
            .Accepts<PastDatePieChartRequest>("application/json")
            .Produces<DashboardDualResponse>(200)
            .Produces(400)
            .WithTags("Dashboard")
            .WithDescription("İki liste döndürür: charts (sabit 12 veri) + operations (değişken veri) Mode 0 aktif vardiya Mode 1: 1 gün öncesi Mode 2: 1 hafta öncesi\n Mode 3: 1 ay " +
            "öncesi Mode 4: Custom, Custom için veriler girilmeli örnek veri formatı:2025-07-01 00:00:00  " +
            " ");

    }

}
