namespace LmMobileApi.Operations.Domain;

public class CurrentOperationsWithFilters
{
    public IEnumerable<CurrentOperation> CurrentOperations { get; set; } = Enumerable.Empty<CurrentOperation>();
    public CurrentOperationFilterOption Filters { get; set; } = new();
}