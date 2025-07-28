using LmMobileApi.Looms.Domain;
using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Looms.Infrastructure.Repositories;

public interface ILoomRepository : IRepository
{
    Task<Result<IEnumerable<Loom>>> GetLoomsCurrentlyStatusAsync(CancellationToken cancellationToken = default);
    Task<Result<Loom>> GetLoomCurrentlyStatusAsync(string loomNo, CancellationToken cancellationToken = default);
}

public class LoomRepository(IUnitOfWork unitOfWork) : DapperRepository(unitOfWork), ILoomRepository
{
    public Task<Result<IEnumerable<Loom>>> GetLoomsCurrentlyStatusAsync(CancellationToken cancellationToken = default)
    {
        const string query = @"SELECT * FROM tvw_mobile_Looms_CurrentlyStatus ORDER BY LoomNo";
        return QueryAsync<Loom>(query, cancellationToken: cancellationToken);
    }

    public async Task<Result<Loom>> GetLoomCurrentlyStatusAsync(string loomNo, CancellationToken cancellationToken = default)
    {
        const string query = @"SELECT * FROM tvw_mobile_Looms_CurrentlyStatus WHERE LoomNo = @LoomNo";
        var loom = await QueryFirstOrDefaultAsync<Loom>(query, new { LoomNo = loomNo }, cancellationToken: cancellationToken);
        if (loom.Data is null)
            return Error.NotFound;
        return loom!;
    }
}