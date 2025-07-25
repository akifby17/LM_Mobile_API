using LmMobileApi.DataManContracts;
using LmMobileApi.Looms.Application.Services;
using LmMobileApi.Looms.Domain;
using LmMobileApi.Shared.Endpoints;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Looms.Presentation;

public class LoomEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/looms/monitoring", async (ILoomService loomService, CancellationToken cancellationToken) =>
        {
            var result = await loomService.GetLoomMonitoringAsync(cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Error);
        })
            .WithName("GetLoomsCurrentlyStatus")
            .Produces<ICollection<Loom>>()
            .WithTags("Looms");

        app.MapPost("/api/looms/changeWeaver", async (ChangeWeaver changeWeaver, ILoomService loomService, CancellationToken cancellationToken) =>
        {

            var result = await loomService.ChangeWeaverAsync(changeWeaver, cancellationToken);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
            .WithName("ChangeWeaver")
            .Produces<Result>()
            .WithTags("Looms");

        app.MapPost("/api/looms/operationStartStop", async (OperationStartStop operationStartStop, ILoomService loomService, CancellationToken cancellationToken) =>
        {

            var result = await loomService.OperationStartStopAsync(operationStartStop, cancellationToken);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
            .WithName("OperationStartStop")
            .Produces<Result>()
            .WithTags("Looms");

        app.MapPost("/api/looms/pieceCutting", async (PieceCutting pieceCutting, ILoomService loomService, CancellationToken cancellationToken) =>
        {

            var result = await loomService.PieceCuttingAsync(pieceCutting, cancellationToken);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
            .WithName("PieceCutting")
            .Produces<Result>()
            .WithTags("Looms");

        app.MapPost("/api/looms/styleWorkOrderStartStopPause", async (StyleWorkOrderStartStopPause styleWorkOrderStartStopPause, ILoomService loomService, CancellationToken cancellationToken) =>
        {

            var result = await loomService.StyleWorkOrderStartStopPauseAsync(styleWorkOrderStartStopPause, cancellationToken);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
            .WithName("StyleWorkOrderStartStopPause")
            .Produces<Result>()
            .WithTags("Looms");

        app.MapPost("/api/looms/warpWorkOrderStartStopPause", async (WarpWorkOrderStartStopPause warpWorkOrderStartStopPause, ILoomService loomService, CancellationToken cancellationToken) =>
        {

            var result = await loomService.WarpWorkOrderStartStopPauseAsync(warpWorkOrderStartStopPause, cancellationToken);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
            .WithName("WarpWorkOrderStartStopPause")
            .Produces<Result>()
            .WithTags("Looms");

        app.MapPost("/api/looms/warpWorkOrder23StartStopPause", async (WarpWorkOrder23StartStopPause warpWorkOrder23StartStopPause, ILoomService loomService, CancellationToken cancellationToken) =>
        {

            var result = await loomService.WarpWorkOrder23StartStopPauseAsync(warpWorkOrder23StartStopPause, cancellationToken);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        })
            .WithName("WarpWorkOrder23StartStopPause")
            .Produces<Result>()
            .WithTags("Looms");
    }
}