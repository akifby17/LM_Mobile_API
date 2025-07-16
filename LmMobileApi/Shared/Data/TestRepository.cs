using LmMobileApi.Shared.Results;

namespace LmMobileApi.Shared.Data;

public interface ITestRepository : IRepository
{
    Task<Result<IEnumerable<string>>> GetTableNamesAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> GetUserCountAsync(CancellationToken cancellationToken = default);
}

public class TestRepository(IUnitOfWork unitOfWork) : DapperRepository(unitOfWork), ITestRepository
{
    public async Task<Result<IEnumerable<string>>> GetTableNamesAsync(CancellationToken cancellationToken = default)
    {
        const string query = @"
            SELECT TABLE_NAME 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE' 
            ORDER BY TABLE_NAME";

        return await QueryAsync<string>(query, cancellationToken: cancellationToken);
    }

    public async Task<Result<int>> GetUserCountAsync(CancellationToken cancellationToken = default)
    {
        const string query = "SELECT COUNT(*) FROM Users";
        var result = await QueryFirstOrDefaultAsync<int>(query, cancellationToken: cancellationToken);
        return result.IsSuccess ? Result<int>.Success(result.Data) : result.Error;
    }
}