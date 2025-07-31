using LmMobileApi.Personnels.Domain;
using LmMobileApi.Personnels.Infrastructure.Repositories;
using LmMobileApi.Shared.Application;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Personnels.Application.Services;

public interface IPersonnelService
{
    Task<Result<IEnumerable<Personnel>>> GetPersonnelsAsync(CancellationToken cancellationToken = default);
}

public class PersonnelService(IPersonnelRepository personnelRepository) : ApplicationService(personnelRepository), IPersonnelService
{
    private IPersonnelRepository PersonnelRepository => Repository as IPersonnelRepository
        ?? throw new InvalidOperationException("Repository is not of type IPersonnelRepository");

    public Task<Result<IEnumerable<Personnel>>> GetPersonnelsAsync(CancellationToken cancellationToken = default)
    {
        return PersonnelRepository.GetPersonnelsAsync(cancellationToken);
    }
}