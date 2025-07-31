using LmMobileApi.Operations.Domain;
using LmMobileApi.Operations.Infrastructure;
using LmMobileApi.Shared.Application;
using LmMobileApi.Shared.Results;

namespace LmMobileApi.Operations.Application.Services;

public interface IOperationService
{
    Task<Result<IEnumerable<Operation>>> GetOperationsAsync(CancellationToken cancellationToken = default);
}

public class OperationService(IOperationRepository operationRepository) : ApplicationService(operationRepository), IOperationService
{
    private IOperationRepository OperationRepository => Repository as IOperationRepository
        ?? throw new InvalidOperationException("Repository is not of type IOperationRepository");

    public Task<Result<IEnumerable<Operation>>> GetOperationsAsync(CancellationToken cancellationToken = default)
    {
        return OperationRepository.GetOperationsAsync(cancellationToken);
    }
}

