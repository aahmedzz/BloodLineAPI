using System.Text;
using System.Text.Json;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Notifications.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetNotificationsQuery, CursorPagedResult<NotificationDto>>
{
    public async Task<CursorPagedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100);

        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == request.UserId)
            .OrderByDescending(n => n.SentDate)
            .ThenByDescending(n => n.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Cursor) && TryDecodeCursor(request.Cursor, out var cursorSentDate, out var cursorId))
        {
            query = query.Where(n => n.SentDate < cursorSentDate || (n.SentDate == cursorSentDate && n.Id.CompareTo(cursorId) < 0));
        }

        var notifications = await query
            .Take(pageSize + 1)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.ActionPayload,
                n.IsRead,
                n.SentDate
            })
            .ToListAsync(cancellationToken);

        var hasMore = notifications.Count > pageSize;
        var items = notifications.Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.Type.ToString(),
                DeserializePayload(n.ActionPayload),
                n.IsRead,
                n.SentDate))
            .ToList();

        var lastItem = notifications.Count > 0
            ? notifications[Math.Min(pageSize, notifications.Count) - 1]
            : null;

        var nextCursor = hasMore && lastItem is not null
            ? EncodeCursor(lastItem.SentDate, lastItem.Id)
            : null;

        return new CursorPagedResult<NotificationDto>(items, nextCursor, hasMore);
    }

    private static Dictionary<string, string>? DeserializePayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(payload);
    }

    private static string EncodeCursor(DateTime sentDate, Guid id)
    {
        var raw = $"{sentDate:O}|{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static bool TryDecodeCursor(string cursor, out DateTime sentDate, out Guid id)
    {
        sentDate = default;
        id = default;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('|', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            if (!DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out sentDate))
            {
                return false;
            }

            return Guid.TryParse(parts[1], out id);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
