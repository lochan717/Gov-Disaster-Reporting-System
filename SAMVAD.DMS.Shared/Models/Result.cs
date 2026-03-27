namespace SAMVAD.DMS.Shared.Models;

public class Result
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }

    public static Result Succeed(string? message = null) => new()
    {
        IsSuccess = true,
        Message = message
    };

    public static Result Fail(string message) => new()
    {
        IsSuccess = false,
        Message = message
    };
}

public sealed class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Succeed(T data, string? message = null) => new()
    {
        IsSuccess = true,
        Data = data,
        Message = message
    };

    public static new Result<T> Fail(string message) => new()
    {
        IsSuccess = false,
        Message = message
    };
}