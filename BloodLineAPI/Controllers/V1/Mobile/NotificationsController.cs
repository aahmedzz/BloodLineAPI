using Asp.Versioning;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Notifications.Commands;
using BloodLineAPI.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using BloodLineAPI.Application.Features.Notifications.Commands.MarkNotificationRead;
using BloodLineAPI.Application.Features.Notifications.Dtos;
using BloodLineAPI.Application.Features.Notifications.Queries.GetNotifications;
using BloodLineAPI.Application.Features.Notifications.Queries.GetUnreadCount;
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
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    public record DeviceTokenRequest(string Token, string Platform);
    public record TestNotificationRequest(string Title, string Message);

    [HttpPost("device-token")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterDeviceToken([FromBody] DeviceTokenRequest request, CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        await sender.Send(new RegisterDeviceTokenCommand(
            donorId,
            request.Token,
            request.Platform), ct);

        return Ok(ApiResponse<string>.Ok("Device token registered successfully."));
    }

    [HttpDelete("device-token")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnregisterDeviceToken([FromQuery] string token, CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        await sender.Send(new UnregisterDeviceTokenCommand(
            donorId,
            token), ct);

        return Ok(ApiResponse<string>.Ok("Device token unregistered successfully."));
    }

    [HttpPost("test-notification")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendTestNotification([FromBody] TestNotificationRequest request, CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        await sender.Send(new SendTestNotificationCommand(donorId, request.Title, request.Message), ct);
        return Ok(ApiResponse<string>.Ok("Test notification sent successfully."));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CursorPagedResult<NotificationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var query = new GetNotificationsQuery
        {
            UserId = donorId,
            Cursor = cursor,
            PageSize = pageSize
        };

        var result = await sender.Send(query, ct);
        return Ok(ApiResponse<CursorPagedResult<NotificationDto>>.Ok(result));
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var count = await sender.Send(new GetUnreadCountQuery(donorId), ct);
        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        await sender.Send(new MarkNotificationReadCommand(donorId, id), ct);
        return Ok(ApiResponse<string>.Ok("Notification marked as read."));
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        if (!TryGetDonorId(out var donorId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid or missing authentication token."));
        }

        var count = await sender.Send(new MarkAllNotificationsReadCommand(donorId), ct);
        return Ok(ApiResponse<int>.Ok(count, $"{count} notifications marked as read."));
    }

    private bool TryGetDonorId(out Guid donorId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out donorId);
    }
}