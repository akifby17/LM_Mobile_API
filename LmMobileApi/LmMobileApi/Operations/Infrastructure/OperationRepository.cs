using LmMobileApi.Operations.Domain;
using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Operations.Infrastructure;

public interface IOperationRepository : IRepository
{
    Task<Result<IEnumerable<Operation>>> GetOperationsAsync(CancellationToken cancellationToken = default);

}
public class OperationRepository(IUnitOfWork unitOfWork) : DapperRepository(unitOfWork), IOperationRepository
{
    public Task<Result<IEnumerable<Operation>>> GetOperationsAsync(CancellationToken cancellationToken = default)
    {
        const string query = @"SELECT Code, Name FROM OperationCodes ORDER BY Code";
        return QueryAsync<Operation>(query, cancellationToken: cancellationToken);
    }
}

