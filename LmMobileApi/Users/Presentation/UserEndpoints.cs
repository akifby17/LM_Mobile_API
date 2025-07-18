using LmMobileApi.Shared.Endpoints;
using LmMobileApi.Users.Domain;
using LmMobileApi.Users.Domain.Contracts;

namespace LmMobileApi.Users.Presentation;

public class UserEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Login endpoint
        app.MapPost("/api/users/login", async (IUserService userService, User request, CancellationToken cancellationToken) =>
        {
            var result = await userService.LoginAsync(request, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Error);
        })
            .WithName("UserLogin")
            .Produces<LoginResponse>()
            .WithTags("Users");

        // Check token endpoint
        app.MapGet("/api/users/check-token", () => Results.Ok())
            .WithName("CheckToken")
            .WithTags("Users")
            .Produces(200)
            .Produces(401)
            .RequireAuthorization();
    }
}