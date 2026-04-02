namespace BloodLineAPI.Application.Common.Models;

/// <summary>
/// Unified API response envelope used by all endpoints.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public object? Errors { get; init; }

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = "Operation completed successfully.")
        => new() { Success = true, Message = message, Data = data };

    /// <summary>
    /// Creates a failure response with an error message.
    /// </summary>
    public static ApiResponse<T> Fail(string message, object? errors = null)
        => new() { Success = false, Message = message, Errors = errors };

    /// <summary>
    /// Creates a failure response with data (e.g., login incomplete profile).
    /// </summary>
    public static ApiResponse<T> FailWithData(string message, T data, object? errors = null)
        => new() { Success = false, Message = message, Data = data, Errors = errors };
}

/// <summary>
/// Non-generic unified API response envelope for error-only responses.
/// </summary>
public class ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }
    public object? Errors { get; init; }

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse Ok(object? data = null, string message = "Operation completed successfully.")
        => new() { Success = true, Message = message, Data = data };

    /// <summary>
    /// Creates a failure response with an error message.
    /// </summary>
    public static ApiResponse Fail(string message, object? errors = null)
        => new() { Success = false, Message = message, Errors = errors };

    /// <summary>
    /// Creates a failure response with data attached.
    /// </summary>
    public static ApiResponse FailWithData(string message, object? data, object? errors = null)
        => new() { Success = false, Message = message, Data = data, Errors = errors };
}
