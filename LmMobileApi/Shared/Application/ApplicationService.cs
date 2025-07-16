using LmMobileApi.Shared.Data;

namespace LmMobileApi.Shared.Application;

public abstract class ApplicationService(IRepository repository)
{
    protected readonly IRepository Repository = repository;
}