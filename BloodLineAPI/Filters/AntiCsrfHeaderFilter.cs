using BloodLineAPI.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BloodLineAPI.Filters;

public class AntiCsrfHeaderFilter : IAsyncActionFilter
{
    private const string ExpectedHeader = "X-Requested-With";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Check if the endpoint is tagged for the System audience
        var isSystemEndpoint = context.ActionDescriptor.EndpointMetadata
            .OfType<ApiAudienceAttribute>()
            .Any(a => a.Audience == Audience.System);

        var method = context.HttpContext.Request.Method;
        var isSafeMethod = string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(method, "TRACE", StringComparison.OrdinalIgnoreCase);

        if (isSystemEndpoint && !isSafeMethod)
        {
            // If it's a System endpoint and not a safe method, enforce the CSRF header
            if (!context.HttpContext.Request.Headers.ContainsKey(ExpectedHeader))
            {
                context.Result = new BadRequestObjectResult(new { message = $"Missing required '{ExpectedHeader}' header for CSRF protection." });
                return;
            }
        }

        await next();
    }
}
