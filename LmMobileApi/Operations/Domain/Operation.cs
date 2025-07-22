namespace LmMobileApi.Operations.Domain;

public class Operation
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    private Operation()
    {
    }
}