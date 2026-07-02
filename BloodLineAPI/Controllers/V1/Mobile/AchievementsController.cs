using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Gamification.Queries.GetAllBadges;
using BloodLineAPI.Application.Features.Gamification.Queries.GetDonorBadgeHistory;
using BloodLineAPI.Application.Features.Gamification.Queries.GetAllTimeLeaderboard;
using BloodLineAPI.Application.Features.Gamification.Queries.GetMonthlyLeaderboard;
using BloodLineAPI.Application.Features.Gamification.Queries.GetDailyInfo;
using BloodLineAPI.Application.Features.Gamification.Commands.ReadDailyInfo;
using BloodLineAPI.Application.Features.Gamification.Commands.ReferDailyInfo;
using BloodLineAPI.Application.Features.Gamification.Queries.GetPointsRules;
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

    [HttpGet("leaderboard/monthly")]
    [ProducesResponseType(typeof(ApiResponse<MonthlyLeaderboardResponseDto>), StatusCodes.Status200OK)]
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

        return Ok(ApiResponse<MonthlyLeaderboardResponseDto>.Ok(result));
    }

    [HttpGet("leaderboard/all-time")]
    [ProducesResponseType(typeof(ApiResponse<AllTimeLeaderboardResponseDto>), StatusCodes.Status200OK)]
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

        return Ok(ApiResponse<AllTimeLeaderboardResponseDto>.Ok(result));
    }

    [HttpGet("badges")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BadgeDetailsDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBadges(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllBadgesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<BadgeDetailsDto>>.Ok(result));
    }

    [HttpGet("badges/history")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BadgeHistoryItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBadgeHistory(CancellationToken cancellationToken)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await sender.Send(new GetDonorBadgeHistoryQuery(donorId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<BadgeHistoryItemDto>>.Ok(result));
    }

    [HttpGet("daily-info")]
    [ProducesResponseType(typeof(ApiResponse<DailyInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDailyInfo(CancellationToken cancellationToken)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await sender.Send(new GetDailyInfoQuery(donorId), cancellationToken);
        return Ok(ApiResponse<DailyInfoDto>.Ok(result));
    }

    [HttpPost("daily-info/read")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReadDailyInfo(CancellationToken cancellationToken)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var result = await sender.Send(new ReadDailyInfoCommand(donorId), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<string>.Ok(result.Data!));
    }

    [HttpGet("daily-info/referred")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReferDailyInfo([FromQuery(Name = "ref")] Guid referrerId, CancellationToken cancellationToken)
    {
        // Credit the referrer (the one who shared) with 50 points
        await sender.Send(new ReferDailyInfoCommand(referrerId), cancellationToken);

        // If the visiting user is authenticated, also credit them with 20 points for reading the tip
        if (TryGetDonorId(out var visitorId))
        {
            await sender.Send(new ReadDailyInfoCommand(visitorId), cancellationToken);
        }

        return Ok(ApiResponse<string>.Ok("Referral registered successfully."));
    }

    [HttpGet("points-rules")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PointRuleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPointsRules(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPointsRulesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PointRuleDto>>.Ok(result));
    }

    private bool TryGetDonorId(out Guid donorId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out donorId);
    }
}
