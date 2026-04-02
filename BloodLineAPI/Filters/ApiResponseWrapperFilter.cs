using BloodLineAPI.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BloodLineAPI.Filters;

/// <summary>
/// Automatically wraps all controller responses in the unified <see cref="ApiResponse{T}"/> envelope.
/// Success responses get wrapped with success=true, error responses with success=false.
/// Responses that are already an <see cref="ApiResponse"/> or <see cref="ApiResponse{T}"/> are left untouched.
/// </summary>
public class ApiResponseWrapperFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult objectResult)
            return;

        var value = objectResult.Value;

        // Already wrapped — skip
        if (value is ApiResponse || IsGenericApiResponse(value))
            return;

        var statusCode = objectResult.StatusCode ?? 200;

        if (statusCode >= 200 && statusCode < 300)
        {
            // Success — wrap the data
            objectResult.Value = ApiResponse.Ok(value);
        }
        else if (value is ValidationProblemDetails validationProblem)
        {
            // Validation error — include field-level errors
            objectResult.Value = ApiResponse.Fail(
                validationProblem.Detail ?? validationProblem.Title ?? "Validation Failed",
                validationProblem.Errors);
        }
        else if (value is ProblemDetails problem)
        {
            // Business/server error
            objectResult.Value = ApiResponse.Fail(
                problem.Detail ?? problem.Title ?? "An error occurred.");
        }
    }

    public void OnResultExecuted(ResultExecutedContext context) { }

    private static bool IsGenericApiResponse(object? value)
    {
        if (value is null) return false;
        var type = value.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResponse<>);
    }
}
