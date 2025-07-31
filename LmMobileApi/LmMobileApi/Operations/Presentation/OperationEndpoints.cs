using LmMobileApi.Operations.Application.Services;
using LmMobileApi.Operations.Domain;
using LmMobileApi.Shared.Endpoints;

namespace LmMobileApi.Operations.Presentation;

public class OperationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/operations", async (IOperationService operationService, CancellationToken cancellationToken) =>
            {
                var result = await operationService.GetOperationsAsync(cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Error);
            })
            .WithName("GetOperationsAsync")
            .Produces<ICollection<Operation>>()
            .WithTags("Operations");
    }
}
