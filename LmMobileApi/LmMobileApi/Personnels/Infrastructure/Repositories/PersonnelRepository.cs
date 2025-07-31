using LmMobileApi.Personnels.Domain;
using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Personnels.Infrastructure.Repositories;

public interface IPersonnelRepository : IRepository
{
    Task<Result<IEnumerable<Personnel>>> GetPersonnelsAsync(CancellationToken cancellationToken = default);
}

public class PersonnelRepository(IUnitOfWork unitOfWork) : DapperRepository(unitOfWork), IPersonnelRepository
{
    public Task<Result<IEnumerable<Personnel>>> GetPersonnelsAsync(CancellationToken cancellationToken = default)
    {
        const string query = @"SELECT PersonnelID, PersonnelName FROM Personnel ORDER BY PersonnelName";
        return QueryAsync<Personnel>(query, cancellationToken: cancellationToken);
    }
}