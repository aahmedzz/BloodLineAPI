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
    [ProducesResponseType(typeof(RegisterMobileUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterMobileUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
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
    [ProducesResponseType(typeof(VerifyOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyMobileRegistrationOtpCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
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
    [ProducesResponseType(typeof(DonorAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Login with national ID or phone number.
    /// </summary>
    /// <remarks>
    /// Authenticates the user and returns JWT access token, refresh token, and user info.
    /// 
    /// **Note:** If the user has not completed their registration profile, the response will be 
    /// a 400 error with `data` containing a temporary token and user info. The Flutter Dev should 
    /// redirect the user to the complete-profile screen.
    /// 
    /// **Success Response (200):**
    /// ```json
    /// {
    ///   "token": "eyJ...",
    ///   "refreshToken": "abc...",
    ///   "user": {
    ///     "userId": "guid",
    ///     "nationalId": "...",
    ///     "phoneNumber": "...",
    ///     "fullName": "...",
    ///     "isPhoneNumberVerified": true,
    ///     "isRegistrationCompleted": true
    ///   }
    /// }
    /// ```
    /// 
    /// **Incomplete Registration Response (400):**
    /// ```json
    /// {
    ///   "message": "Please complete your registration profile first.",
    ///   "data": {
    ///     "token": "temporary-token",
    ///     "refreshToken": "",
    ///     "user": { ... }
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpPost("Login")]
    [ProducesResponseType(typeof(DonorAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginFailureResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginMobileUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Data is not null)
            {
                return BadRequest(new LoginFailureResponse(result.Error!, result.Data));
            }

            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
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
    [ProducesResponseType(typeof(DonorAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
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
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotMobilePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(new MessageResponse(result.Data!));
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
    [ProducesResponseType(typeof(VerifyResetOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyResetOtp([FromBody] VerifyForgotPasswordOtpCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        var parts = result.Data?.Split('|', 2) ?? Array.Empty<string>();
        if (parts.Length != 2)
        {
            return Problem(title: "Bad Request", detail: "Invalid reset token response.", statusCode: StatusCodes.Status400BadRequest);
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
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetMobilePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: "Bad Request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(new MessageResponse(result.Data!));
    }

}
