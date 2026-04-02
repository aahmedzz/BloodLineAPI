using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Auth.Commands.CompleteMobileRegistrationProfile;
using BloodLineAPI.Application.Features.Auth.Commands.ForgotMobilePassword;
using BloodLineAPI.Application.Features.Auth.Commands.LoginMobileUser;
using BloodLineAPI.Application.Features.Auth.Commands.RefreshToken;
using BloodLineAPI.Application.Features.Auth.Commands.RegisterMobileUser;
using BloodLineAPI.Application.Features.Auth.Commands.ResetMobilePassword;
using BloodLineAPI.Application.Features.Auth.Commands.VerifyForgotPasswordOtp;
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
[Produces("application/json")]
public class AuthController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;
   

    /// <summary>
    /// Register a new mobile user.
    /// </summary>
    /// <remarks>
    /// Creates a new user account and sends an OTP to the user's phone number for verification.
    /// 
    /// **Flow:** Register → Verify OTP → Complete Profile → Login
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterMobileUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterMobileUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Fail(result.Error!));
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Verify the registration OTP code.
    /// </summary>
    /// <remarks>
    /// Verifies the OTP sent during registration. Returns a temporary JWT token 
    /// that must be used in the Authorization header for the complete-profile step.
    /// 
    /// **Flow:** Register → **Verify OTP** → Complete Profile → Login
    /// </remarks>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(ApiResponse<VerifyOtpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyMobileRegistrationOtpCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Fail(result.Error!));
        }

        return Ok(new VerifyOtpResponse(result.Data!));
    }

    /// <summary>
    /// Complete the registration profile.
    /// </summary>
    /// <remarks>
    /// Completes the donor profile with personal details (blood type, date of birth, etc.).
    /// Requires the temporary JWT token from the verify-otp step in the Authorization header.
    /// Returns the full authentication response with access and refresh tokens.
    /// 
    /// **Flow:** Register → Verify OTP → **Complete Profile** → Login
    /// </remarks>
    [Authorize]
    [HttpPost("complete-profile")]
    [ProducesResponseType(typeof(ApiResponse<DonorAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CompleteProfile([FromBody] CompleteMobileRegistrationProfileCommand command, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Fail("Invalid or missing authentication token."));
        }

        var result = await _sender.Send(command with { UserId = userId }, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Fail(result.Error!));
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Login with national ID or phone number.
    /// </summary>
    /// <remarks>
    /// Authenticates the user and returns JWT access token, refresh token, and user info.
    /// 
    /// **Note:** If the user has not completed their registration profile, the response will be 
    /// a 400 error with `data` containing the partial auth info. The Flutter client should 
    /// redirect the user to the complete-profile screen.
    /// </remarks>
    [HttpPost("Login")]
    [ProducesResponseType(typeof(ApiResponse<DonorAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginMobileUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Data is not null)
            {
                return BadRequest(ApiResponse.FailWithData(result.Error!, result.Data));
            }

            return BadRequest(ApiResponse.Fail(result.Error!));
        }
        return Ok(result.Data);
    }

    /// <summary>
    /// Refresh the JWT access token.
    /// </summary>
    /// <remarks>
    /// Uses an expired access token and a valid refresh token to obtain a new token pair.
    /// Both the access token and refresh token are rotated.
    /// </remarks>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<DonorAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Fail(result.Error!));
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Request a password reset OTP.
    /// </summary>
    /// <remarks>
    /// Sends an OTP to the user's registered phone number for password reset.
    /// 
    /// **Flow:** Forgot Password → Verify Reset OTP → Reset Password
    /// </remarks>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotMobilePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Fail(result.Error!));
        }

        return Ok(ApiResponse.Ok(message: result.Data!));
    }

    /// <summary>
    /// Verify the password reset OTP code.
    /// </summary>
    /// <remarks>
    /// Verifies the OTP sent during forgot-password. Returns the user ID and a reset token 
    /// that must be used in the reset-password step.
    /// 
    /// **Flow:** Forgot Password → **Verify Reset OTP** → Reset Password
    /// </remarks>
    [HttpPost("verify-reset-otp")]
    [ProducesResponseType(typeof(ApiResponse<VerifyResetOtpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyResetOtp([FromBody] VerifyForgotPasswordOtpCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Fail(result.Error!));
        }

        var parts = result.Data?.Split('|', 2) ?? Array.Empty<string>();
        if (parts.Length != 2)
        {
            return BadRequest(ApiResponse.Fail("Invalid reset token response."));
        }

        return Ok(new VerifyResetOtpResponse(parts[0], parts[1]));
    }

    /// <summary>
    /// Reset the user's password.
    /// </summary>
    /// <remarks>
    /// Resets the password using the user ID and reset token from the verify-reset-otp step.
    /// 
    /// **Flow:** Forgot Password → Verify Reset OTP → **Reset Password**
    /// </remarks>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetMobilePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Fail(result.Error!));
        }

        return Ok(ApiResponse.Ok(message: result.Data!));
    }

}
