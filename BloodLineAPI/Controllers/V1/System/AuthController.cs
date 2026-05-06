using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.Auth;
using BloodLineAPI.Application.Features.Auth.Commands.LoginStaffUser;
using BloodLineAPI.Application.Features.Auth.Commands.RefreshStaffToken;
using BloodLineAPI.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BloodLineAPI.Controllers.V1.System;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system/[controller]")]
[ApiAudience(Audience.System)]
[Produces("application/json")]
public class AuthController(ISender sender) : ControllerBase
{
    private const string AccessTokenCookie = "access_token";
    private const string RefreshTokenCookie = "refresh_token";

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginStaffUserCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Error!));

        var data = result.Data!;

        SetTokenCookies(data.Token, data.RefreshToken);
        
        // Return only user info in JSON body
        return Ok(ApiResponse<AuthenticatedStaffUser>.Ok(data.User));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        // 1. EXTRACT FROM COOKIES: Read tokens directly from HttpOnly cookies
        var accessToken = Request.Cookies[AccessTokenCookie];
        var refreshToken = Request.Cookies[RefreshTokenCookie];

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(ApiResponse<object>.Fail("No authentication cookies found."));

        var command = new RefreshStaffTokenCommand(accessToken, refreshToken);
        var result = await sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            ClearTokenCookies();
            return Unauthorized(ApiResponse<object>.Fail(result.Error!));
        }

        var data = result.Data!;
        SetTokenCookies(data.Token, data.RefreshToken);
        return Ok(ApiResponse<AuthenticatedStaffUser>.Ok(data.User));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        ClearTokenCookies();
        return Ok(ApiResponse<object>.Ok(null!, message: "Logged out successfully."));
    }

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        // 2. PATH ISOLATION: Scoped tightly to minimize attack surface
        Response.Cookies.Append(AccessTokenCookie, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Ensure we use HTTPS in production
            SameSite = SameSiteMode.Strict,
            Path = "/api", // Available to all API endpoints
            Expires = DateTimeOffset.UtcNow.AddMinutes(60) 
        });

        Response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Ensure we use HTTPS in production
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/system/auth", // Restricted exclusively to the auth controller
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private void ClearTokenCookies()
    {
        Response.Cookies.Delete(AccessTokenCookie, new CookieOptions { Path = "/api" });
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions { Path = "/api/v1/system/auth" });
    }
}
