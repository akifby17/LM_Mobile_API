using LmMobileApi.Personnels.Application.Services;
using LmMobileApi.Personnels.Domain;
using LmMobileApi.Shared.Endpoints;

namespace LmMobileApi.Personnels.Presentation;

public class PersonnelEndpoints : IEndpoint
    
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {

        app.MapGet("/api/personnels", async (IPersonnelService personnelService, CancellationToken cancellationToken) =>
        {
            var result = await personnelService.GetPersonnelsAsync(cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Data) : Results.BadRequest(result.Error);
        })
            .WithName("GetPersonnelsAsync")
            .Produces<ICollection<Personnel>>()
            .WithTags("Personnels")
            .RequireAuthorization();//Yetkisiz giriþleri engellemek için
    }
}