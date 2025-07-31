namespace LmMobileApi.Shared.Results;

public record Result
{
    public bool IsSuccess { get; init; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; init; } = Error.None;

    // Implicit conversion from Result to bool
    public static implicit operator bool(Result result) => result.IsSuccess;
    // Implicit conversion from bool to Result
    public static implicit operator Result(bool isSuccess) => new() { IsSuccess = isSuccess };
    // Implicit conversion from Error to Result
    public static implicit operator Result(Error error) => new() { Error = error, IsSuccess = false };
    // Implicit conversion from Result to Error
    public static implicit operator Error(Result result) => result.Error!;

    // Success and Failure methods
    public static Result Success => new() { IsSuccess = true };
    public static Result Failure(Error error) => new() { Error = error, IsSuccess = false };
    public static Result Failure(string code, string description) => new() { Error = new Error(code, description), IsSuccess = false };
    public static Result Failure(string code) => new() { Error = new Error(code, string.Empty), IsSuccess = false };

    // Match method
    public void Match(Action onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess)
            onSuccess();
        else
            onFailure(Error!);
    }
}

public record Result<T>
{
    public T? Data { get; private init; }
    public bool IsSuccess { get; private init; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; private init; } = Error.None;

    // Implicit conversion from Result<T> to T
    public static implicit operator T(Result<T> result) => result.Data!;
    // Implicit conversion from T to Result<T>
    public static implicit operator Result<T>(T data) => new() { Data = data, IsSuccess = true };
    // Implicit conversion from Result<T> to Result
    public static implicit operator Result(Result<T> result) => new() { Error = result.Error, IsSuccess = result.IsSuccess };
    // Implicit conversion from Result to Result<T>
    public static implicit operator Result<T>(Result result) => new() { Error = result.Error, IsSuccess = result.IsSuccess };
    // Implicit conversion from Error to Result<T>
    public static implicit operator Result<T>(Error error) => new() { Error = error, IsSuccess = false };
    // Implicit conversion from Result<T> to Error
    public static implicit operator Error(Result<T> result) => result.Error!;
    // Implicit conversion from Result<T> to bool
    public static implicit operator bool(Result<T> result) => result.IsSuccess;
    // Implicit conversion from bool to Result<T>
    public static implicit operator Result<T>(bool isSuccess) => new() { IsSuccess = isSuccess };

    // Success and Failure methods
    public static Result<T> Success(T data) => new() { Data = data, IsSuccess = true };
    public static Result<T> Failure(Error error) => new() { Error = error, IsSuccess = false };
    public static Result<T> Failure(string code, string description) => new() { Error = new Error(code, description), IsSuccess = false };
    public static Result<T> Failure(string code) => new() { Error = new Error(code, string.Empty), IsSuccess = false };

    // Match method
    public void Match(Action<T> onSuccess, Action<Error> onFailure)
    {
        if (IsSuccess)
            onSuccess(Data!);
        else
            onFailure(Error!);
    }

    // Match<TOut> method
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        return IsSuccess ? onSuccess(Data!) : onFailure(Error!);
    }

    // Map method
    public Result<TOut> Map<TOut>(Func<T, TOut> map)
    {
        if (IsSuccess)
            return map(Data!);
        return Error!;
    }
}
