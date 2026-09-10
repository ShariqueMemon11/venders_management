namespace Shared.Application.Common.Models;

public class Result<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public static Result<T> SuccessResult(T data, string? message = null) 
        => new() { Success = true, Data = data, Message = message };

    public static Result<T> FailureResult(string message, List<string>? errors = null) 
        => new() { Success = false, Message = message, Errors = errors ?? new() };
}

