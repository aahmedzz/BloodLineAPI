using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Notifications.Dtos;
using MediatR;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery : IRequest<CursorPagedResult<NotificationDto>>
{
    [JsonIgnore]
    public Guid UserId { get; init; }

    public string? Cursor { get; init; }
    public int PageSize { get; init; } = 20;
}

