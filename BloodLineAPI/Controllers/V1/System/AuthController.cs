using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.Auth;
using BloodLineAPI.Application.Features.Auth.Commands.ChangeStaffPassword;
using BloodLineAPI.Application.Features.Auth.Commands.LoginStaffUser;
using BloodLineAPI.Application.Features.Auth.Commands.RefreshStaffToken;
using BloodLineAPI.Application.Features.Auth.Queries.GetCurrentStaffUser;
using BloodLineAPI.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

    /// <summary>
    /// Login with email and password. Tokens are set as HttpOnly cookies.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthenticatedStaffUser>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
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

    /// <summary>
    /// Get the currently authenticated staff user's info from session cookie.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AuthenticatedStaffUser>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));

        var result = await sender.Send(new GetCurrentStaffUserQuery(userId), ct);
        if (!result.IsSuccess)
            return Unauthorized(ApiResponse<object>.Fail(result.Error!));

        return Ok(ApiResponse<AuthenticatedStaffUser>.Ok(result.Data!));
    }

    /// <summary>
    /// Rotate tokens via HttpOnly cookies. No request body needed.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthenticatedStaffUser>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        var accessToken = Request.Cookies[AccessTokenCookie];
        var refreshToken = Request.Cookies[RefreshTokenCookie];

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(ApiResponse<object>.Fail("No authentication cookies found."));

        var command = new RefreshStaffTokenCommand(accessToken, refreshToken);
        var result = await sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            return Unauthorized(ApiResponse<object>.Fail(result.Error!));
        }

        var data = result.Data!;
        SetTokenCookies(data.Token, data.RefreshToken);
        return Ok(ApiResponse<AuthenticatedStaffUser>.Ok(data.User));
    }

    /// <summary>
    /// Change the authenticated user's password.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeStaffPasswordCommand command, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));

        var result = await sender.Send(command with { UserId = userId }, ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Error!));

        return Ok(ApiResponse<object>.Ok(null!, message: result.Data!));
    }

    /// <summary>
    /// Logout by clearing authentication cookies.
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        ClearTokenCookies();
        return Ok(ApiResponse<object>.Ok(null!, message: "Logged out successfully."));
    }

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        Response.Cookies.Append(AccessTokenCookie, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Ensure we use HTTPS in production
            SameSite = SameSiteMode.None,
            Path = "/", // Available to all API endpoints
            Expires = DateTimeOffset.UtcNow.AddDays(7) // Must outlive the JWT so the refresh endpoint can read the expired token
        });

        Response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Ensure we use HTTPS in production
            SameSite = SameSiteMode.None,
            Path = "/", // Available to all API endpoints
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private void ClearTokenCookies()
    {
        Response.Cookies.Delete(AccessTokenCookie, new CookieOptions
        {
            Path = "/",
            SameSite = SameSiteMode.None,
            Secure = true
        });
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            Path = "/",
            SameSite = SameSiteMode.None,
            Secure = true
        });
    }
}
