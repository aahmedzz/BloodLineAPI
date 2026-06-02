using Hangfire.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using BloodLineAPI.Domain.Entities.Users;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BloodLineAPI.Filters;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // 1. Allow local requests (development / local machine)
        var connection = httpContext.Connection;
        if (connection.RemoteIpAddress != null)
        {
            if (connection.LocalIpAddress != null)
            {
                if (connection.RemoteIpAddress.Equals(connection.LocalIpAddress))
                {
                    return true;
                }
            }
            if (System.Net.IPAddress.IsLoopback(connection.RemoteIpAddress))
            {
                return true;
            }
        }

        // 2. Allow fallback query parameter token (?token=YOUR_SECRET)
        var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var configuredToken = configuration["Hangfire:DashboardToken"];
        if (!string.IsNullOrWhiteSpace(configuredToken))
        {
            var requestToken = httpContext.Request.Query["token"].ToString();
            if (string.Equals(requestToken, configuredToken, StringComparison.Ordinal))
            {
                return true;
            }
        }

        // 3. Allow authenticated Admin users (checks standard JWT claims if cookie is sent)
        if (httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("Admin"))
        {
            return true;
        }

        // 4. Basic Auth fallback (authenticating against the ASP.NET Core Identity database)
        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var credentials = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(authHeader.Substring(6))
                ).Split(':');

                if (credentials.Length == 2)
                {
                    var emailOrUsername = credentials[0]?.Trim();
                    var pass = credentials[1];

                    if (!string.IsNullOrWhiteSpace(emailOrUsername) && !string.IsNullOrWhiteSpace(pass))
                    {
                        var isAuthorized = Task.Run(async () =>
                        {
                            var userManager = httpContext.RequestServices.GetRequiredService<UserManager<User>>();
                            var signInManager = httpContext.RequestServices.GetRequiredService<SignInManager<User>>();

                            // Try to find the user by email or username (national ID)
                            var user = await userManager.FindByEmailAsync(emailOrUsername) 
                                       ?? await userManager.FindByNameAsync(emailOrUsername);

                            if (user == null || user.IsDeleted)
                            {
                                return false;
                            }

                            // Verify password hash securely
                            var signInResult = await signInManager.CheckPasswordSignInAsync(user, pass, lockoutOnFailure: false);
                            if (!signInResult.Succeeded)
                            {
                                return false;
                            }

                            // Check that the user has the Admin role
                            var roles = await userManager.GetRolesAsync(user);
                            return roles.Contains("Admin");
                        }).GetAwaiter().GetResult();

                        if (isAuthorized)
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing/decoding errors
            }
        }

        // Prompt browser for Basic authentication credentials if not authorized
        httpContext.Response.StatusCode = 401;
        if (!httpContext.Response.Headers.ContainsKey("WWW-Authenticate"))
        {
            httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
        }

        return false;
    }
}
