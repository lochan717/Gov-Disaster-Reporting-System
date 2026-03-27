namespace SAMVAD.DMS.Shared.Models;

public class ApiResponseDto<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }

    public static ApiResponseDto<T> Ok(T? data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponseDto<T> Error(string message) => new()
    {
        Success = false,
        Message = message
    };
}