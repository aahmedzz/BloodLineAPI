using Asp.Versioning;
using BloodLineAPI.Application.Features.Auth.Commands.CompleteMobileRegistrationProfile;
using BloodLineAPI.Application.Features.Auth.Commands.LoginMobileUser;
using BloodLineAPI.Application.Features.Auth.Commands.RefreshToken;
using BloodLineAPI.Application.Features.Auth.Commands.RegisterMobileUser;
using BloodLineAPI.Application.Features.Auth.Commands.VerifyMobileRegistrationOtp;
using BloodLineAPI.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;   

namespace BloodLineAPI.Controllers.V1.Mobile;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mobile/[controller]")]
[ApiAudience(Audience.Mobile)]
public class AuthController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;
   

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

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyMobileRegistrationOtpCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(new { temporaryToken = result.Data });
    }

    [Authorize]
    [HttpPost("complete-profile")]
    public async Task<IActionResult> CompleteProfile([FromBody] CompleteMobileRegistrationProfileCommand command, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(command with { UserId = userId }, cancellationToken);
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
            if (result.Data is not null)
            {
                return BadRequest(new { message = result.Error, data = result.Data });
            }

            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }
        return Ok(result.Data);
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
}
