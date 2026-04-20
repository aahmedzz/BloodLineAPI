using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Gamification.Queries.GetAllBadges;
using BloodLineAPI.Application.Features.Gamification.Queries.GetAllTimeLeaderboard;
using BloodLineAPI.Application.Features.Gamification.Queries.GetDonorGamificationProfile;
using BloodLineAPI.Application.Features.Gamification.Queries.GetMonthlyLeaderboard;
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
public sealed class AchievementsController(ISender sender) : ControllerBase
{
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<DonorGamificationProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await sender.Send(new GetDonorGamificationProfileQuery(donorId), cancellationToken);
        return Ok(ApiResponse<DonorGamificationProfileDto>.Ok(result));
    }

    [HttpGet("leaderboard/monthly")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MonthlyLeaderboardEntryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMonthlyLeaderboard(
        [FromQuery] int top = 10,
        [FromQuery] bool onlyMyDistrict = false,
        [FromQuery] bool onlyMyArea = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await sender.Send(
            new GetMonthlyLeaderboardQuery(donorId, top, onlyMyDistrict, onlyMyArea),
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<MonthlyLeaderboardEntryDto>>.Ok(result));
    }

    [HttpGet("leaderboard/all-time")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AllTimeLeaderboardEntryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllTimeLeaderboard(
        [FromQuery] int top = 10,
        [FromQuery] bool onlyMyDistrict = false,
        [FromQuery] bool onlyMyArea = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await sender.Send(
            new GetAllTimeLeaderboardQuery(donorId, top, onlyMyDistrict, onlyMyArea),
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<AllTimeLeaderboardEntryDto>>.Ok(result));
    }

    [HttpGet("badges")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BadgeListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBadges(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllBadgesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<BadgeListItemDto>>.Ok(result));
    }

    private bool TryGetDonorId(out Guid donorId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out donorId);
    }
}
