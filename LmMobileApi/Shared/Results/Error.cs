namespace LmMobileApi.Shared.Results;

public record Error(string Code, string Description)
{
    public static Error None => new(string.Empty, string.Empty);
    public static Error Unauthorized => new("Unauthorized", "The request was not authorized.");
    public static Error NotFound => new("NotFound", "The requested resource was not found.");
    public static Error BadRequest => new("BadRequest", "The request was invalid.");
    public static Error InternalServerError => new("InternalServerError", "An error occurred while processing the request.");
}