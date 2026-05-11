using System.Net;
using System.Text.Json;
using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Middleware;

public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation error occurred");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = ApiResponse.Fail("Validation Failed", ex.Errors);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json";

            var response = ApiResponse.Fail(ex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Resource key not found");
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json";

            var response = ApiResponse.Fail(ex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Domain rule violation");
            context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            context.Response.ContentType = "application/json";

            var response = ApiResponse.Fail(ex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict detected");
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";

            var response = ApiResponse.Fail("This time slot was just booked by another donor. Please select a different slot.");
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = ApiResponse.Fail("An unexpected error occurred.");
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }
}
