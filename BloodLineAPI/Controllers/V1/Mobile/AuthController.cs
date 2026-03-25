using Asp.Versioning;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Auth.Commands.ForgetAndResetPasswordDto;
using BloodLineAPI.Application.Features.Auth.Commands.LoginMobileUser;
using BloodLineAPI.Application.Features.Auth.Commands.RefreshToken;
using BloodLineAPI.Application.Features.Auth.Commands.RegisterMobileUser;
using BloodLineAPI.Attributes;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BloodLineAPI.Controllers.V1.Mobile;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mobile/[controller]")]
[ApiAudience(Audience.Mobile)]
public class AuthController(ISender sender, IJwtGenerator jwtGenerator, UserManager<User> userManager, ILogger<AuthController> logger) : ControllerBase
{
    private readonly ISender _sender = sender;
    private readonly IJwtGenerator _jwtGenerator = jwtGenerator;
    private readonly UserManager<User> _userManager = userManager;
    private readonly ILogger<AuthController> _logger = logger;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterMobileUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(result.Data);
    }
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginMobileUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }
        return Ok(result.Data);
    }
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

        if (user == null)
            return Ok();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        _logger.LogInformation("Password reset token for {UserId}: {Token}", user.Id, token);

        return Ok("If the phone number exists, a reset code has been sent.");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

        if (user == null)
            return BadRequest("Invalid request.");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);

        if (!result.Succeeded)
            return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));

        return Ok("Password has been reset successfully.");
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(result.Data);
    }

    [HttpGet("test")]
    [Authorize]
    public IActionResult Test()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(title: "Unauthorized", detail: "Missing or invalid authorization header.", statusCode: StatusCodes.Status401Unauthorized);
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        var principal = _jwtGenerator.GetPrincipalFromExpiredToken(token);
        if (principal is null)
        {
            return Problem(title: "Bad Request", detail: "Invalid token.", statusCode: StatusCodes.Status400BadRequest);
        }

        var username = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                       ?? principal.Identity?.Name;

        return Ok(new { Username = username});
    }
}
