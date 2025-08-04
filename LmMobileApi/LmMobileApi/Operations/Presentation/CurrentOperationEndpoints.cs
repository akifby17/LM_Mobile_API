using LmMobileApi.Operations.Application.Services;
using LmMobileApi.Operations.Domain;
using LmMobileApi.Shared.Endpoints;

namespace LmMobileApi.Operations.Presentation;

public class CurrentOperationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // 🚀 NEW: Mode-based endpoint - Tek endpoint tüm işlemleri karşılar
        app.MapPost("/api/current-operations", async (CurrentOperationRequest request, ICurrentOperationService operationService, CancellationToken cancellationToken) =>
            {
                try
                {
                    Console.WriteLine($"📥 Operations API Request - Mode: {request.Mode}, Filter: {request.Filter?.OperationGroupCode ?? "null"}");
                    
                    var result = await operationService.GetCurrentOperationsByModeAsync(request, cancellationToken);
                    
                    Console.WriteLine($"📤 Operations API Response - Success: {result.IsSuccess}, Error: {(result.Error != null ? result.Error.ToString() : "none")}");
                    
                    return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Error);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Operations API Exception: {ex.Message}");
                    Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                    return Results.BadRequest($"Exception: {ex.Message}");
                }
            })
            .WithName("GetCurrentOperationsByMode")
            .WithSummary("Mode-based Current Operations endpoint")
            .WithDescription("Mode 0: Tüm veriler + filtreler, Mode 1: Filtrelenmiş veriler + filtreler")
            .Produces<CurrentOperationsWithFilters>()
            .WithTags("CurrentOperations")
            .RequireAuthorization();

        // 📚 LEGACY: Backward compatibility endpoints (opsiyonel, kaldırılabilir)
        app.MapGet("/api/current-operations/legacy/all", async (ICurrentOperationService operationService, CancellationToken cancellationToken) =>
            {
                var result = await operationService.GetCurrentOperationsAsync(cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Error);
            })
            .WithName("GetCurrentOperationsLegacy")
            .Produces<IEnumerable<CurrentOperation>>()
            .WithTags("CurrentOperations-Legacy")
            .RequireAuthorization();

        app.MapGet("/api/current-operations/legacy/filters", async (ICurrentOperationService operationService, CancellationToken cancellationToken) =>
            {
                var result = await operationService.GetFilterOptionsAsync(cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Error);
            })
            .WithName("GetCurrentOperationFiltersLegacy")
            .Produces<IEnumerable<CurrentOperationFilterOption>>()
            .WithTags("CurrentOperations-Legacy")
            .RequireAuthorization();
    }
}