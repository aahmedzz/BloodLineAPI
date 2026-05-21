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

        if (isSystemEndpoint)
        {
            // If it's a System endpoint, enforce the CSRF header
            if (!context.HttpContext.Request.Headers.ContainsKey(ExpectedHeader))
            {
                context.Result = new BadRequestObjectResult(new { message = $"Missing required '{ExpectedHeader}' header for CSRF protection." });
                return;
            }
        }

        await next();
    }
}
