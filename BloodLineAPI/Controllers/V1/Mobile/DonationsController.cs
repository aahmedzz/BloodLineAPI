using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Donations.Queries.GetMobileDonationHistory;
using BloodLineAPI.Application.Features.Donations.Queries.GetMobileLabResults;
using BloodLineAPI.Application.Features.Donations.Commands.SubmitDonationRating;
using BloodLineAPI.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BloodLineAPI.Controllers.V1.Mobile;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mobile/[controller]")]
[ApiAudience(Audience.Mobile)]
[Produces("application/json")]
[Authorize]
public class DonationsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    /// <summary>
    /// Fetch donation history of the authenticated donor (Mobile app).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DonationHistoryItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDonationHistory([FromQuery] string? donationType, CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await _sender.Send(new GetMobileDonationHistoryQuery(donorId, donationType), ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<IReadOnlyList<DonationHistoryItemDto>>.Ok(result.Data!));
    }

    /// <summary>
    /// Fetch lab results for a specific completed donation (Mobile app).
    /// </summary>
    [HttpGet("{id:guid}/lab-results")]
    [ProducesResponseType(typeof(ApiResponse<MobileLabResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLabResults(Guid id, CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await _sender.Send(new GetMobileLabResultsQuery(id, donorId), ct);
        if (!result.IsSuccess)
        {
            if (result.Error == "Donation appointment not found.")
            {
                return NotFound(ApiResponse<object>.Fail(result.Error));
            }
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<MobileLabResultResponse>.Ok(result.Data!));
    }

    /// <summary>
    /// Submit a rating and optional feedback for a completed donation (Mobile app).
    /// </summary>
    [HttpPost("{id:guid}/rate")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RateDonation(Guid id, [FromBody] RateDonationRequest request, CancellationToken ct)
    {
        if (!TryGetDonorId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await _sender.Send(new SubmitDonationRatingCommand(id, userId, request.StarScore, request.FeedbackText), ct);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<Unit>.Ok(result.Data));
    }

    private bool TryGetDonorId(out Guid donorId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out donorId);
    }
}

public sealed record RateDonationRequest(
    int StarScore,
    string? FeedbackText
);
