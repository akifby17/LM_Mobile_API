using LmMobileApi.Style.Application.Services;
using LmMobileApi.Style.Domain;
using LmMobileApi.Shared.Endpoints;

namespace LmMobileApi.Style.Presentation;

public class StyleWorkOrderEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // 🚀 NEW: Mode-based endpoint - Tek endpoint tüm işlemleri karşılar
        app.MapPost("/api/style-work-orders", async (StyleWorkOrderRequest request, IStyleWorkOrderService styleService, CancellationToken cancellationToken) =>
            {
                try
                {
                    Console.WriteLine($"📥 Style API Request - Mode: {request.Mode}, Filter: {request.Filter?.LoomGroupName ?? "null"}");
                    
                    var result = await styleService.GetStyleWorkOrdersByModeAsync(request, cancellationToken);
                    
                    Console.WriteLine($"📤 Style API Response - Success: {result.IsSuccess}, Error: {(result.Error != null ? result.Error.ToString() : "none")}");
                    
                    return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Error);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Style API Exception: {ex.Message}");
                    Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                    return Results.BadRequest($"Exception: {ex.Message}");
                }
            })
            .WithName("GetStyleWorkOrdersByMode")
            .WithSummary("Mode-based Style Work Orders endpoint")
            .WithDescription("Mode 0: Tüm veriler + filtreler, Mode 1: Filtrelenmiş veriler + filtreler")
            .Produces<StyleWorkOrdersWithFilters>()
            .WithTags("StyleWorkOrders")
            .RequireAuthorization();

        // 📚 LEGACY: Backward compatibility endpoints (opsiyonel, kaldırılabilir)
        app.MapGet("/api/style-work-orders/legacy/all", async (IStyleWorkOrderService styleService, CancellationToken cancellationToken) =>
            {
                var result = await styleService.GetStyleWorkOrdersAsync(cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Error);
            })
            .WithName("GetStyleWorkOrdersLegacy")
            .Produces<IEnumerable<StyleWorkOrder>>()
            .WithTags("StyleWorkOrders-Legacy")
            .RequireAuthorization();

        app.MapGet("/api/style-work-orders/legacy/filters", async (IStyleWorkOrderService styleService, CancellationToken cancellationToken) =>
            {
                var result = await styleService.GetFilterOptionsAsync(cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Error);
            })
            .WithName("GetStyleWorkOrderFiltersLegacy")
            .Produces<IEnumerable<StyleWorkOrderFilterOption>>()
            .WithTags("StyleWorkOrders-Legacy")
            .RequireAuthorization();
    }
} 