using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Donors.Commands.UpdateMobileProfile;
using BloodLineAPI.Application.Features.Donors.Commands.UpdateDonorLocation;
using BloodLineAPI.Application.Features.Donors.Queries.GetMyEligibility;
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
[Authorize]
public class DonorsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    /// <summary>
    /// Update the authenticated donor's profile fields.
    /// </summary>
    /// <remarks>
    /// Allows the mobile donor to update editable fields: birth date, phone number, weight, and address components.
    /// Non-editable fields like Full Name, National ID, Blood Type, and Gender must not be included.
    /// </remarks>
    [HttpPatch("me")]
    [ProducesResponseType(typeof(ApiResponse<MobileUserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateMobileProfileCommand command, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        // Enforce UserId from the token claim rather than the body
        var result = await _sender.Send(command with { UserId = userId }, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<MobileUserProfileResponse>.Ok(result.Data!));
    }

    /// <summary>
    /// Check the authenticated donor's eligibility status.
    /// </summary>
    /// <remarks>
    /// Evaluates if the donor is eligible to donate whole blood, checks recovery progress, and indicates cooldown or ineligible states.
    /// </remarks>
    [HttpGet("me/eligibility")]
    [ProducesResponseType(typeof(ApiResponse<MyEligibilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEligibility(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await _sender.Send(new GetMyEligibilityQuery(userId), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<MyEligibilityResponse>.Ok(result.Data!));
    }

    /// <summary>
    /// Update the authenticated donor's location coordinates.
    /// </summary>
    /// <remarks>
    /// Allows the mobile app to automatically submit the device's latitude and longitude to save in the database.
    /// </remarks>
    [HttpPut("me/location")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateDonorLocationCommand command, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await _sender.Send(command with { UserId = userId }, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<string>.Ok(result.Data!));
    }
}
