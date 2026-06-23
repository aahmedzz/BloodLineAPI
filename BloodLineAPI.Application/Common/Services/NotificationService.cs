using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Common.Services;

public class NotificationService(
    IApplicationDbContext dbContext,
    IPushNotificationDispatcher pushDispatcher,
    IDateTimeProvider dateTimeProvider)
    : INotificationService
{
    public async Task SendNotificationAsync(
        Guid donorId,
        string title,
        string message,
        NotificationType type,
        Dictionary<string, string>? payload = null,
        CancellationToken cancellationToken = default)
    {
        var userId = await dbContext.Donors
            .Where(d => d.Id == donorId)
            .Select(d => d.User.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId == Guid.Empty)
        {
            return;
        }

        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ActionPayload = payload != null ? JsonSerializer.Serialize(payload) : null,
            SentDate = dateTimeProvider.UtcNow,
            IsSent = false
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        var sent = await pushDispatcher.SendAsync(donorId, title, message, payload, cancellationToken);
        if (sent)
        {
            notification.IsSent = true;
            notification.SentVia = "fcm";
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SendBatchNotificationAsync(
        IEnumerable<Guid> donorIds,
        string title,
        string message,
        NotificationType type,
        Dictionary<string, string>? payload = null,
        CancellationToken cancellationToken = default)
    {
        var donorIdsList = donorIds.ToList();
        if (donorIdsList.Count == 0) return;

        var donorUserMap = await dbContext.Donors
            .Where(d => donorIdsList.Contains(d.Id))
            .Select(d => new { DonorId = d.Id, UserId = d.User.Id })
            .ToListAsync(cancellationToken);

        if (donorUserMap.Count == 0) return;

        var payloadStr = payload != null ? JsonSerializer.Serialize(payload) : null;
        var now = dateTimeProvider.UtcNow;

        var notifications = donorUserMap.Select(map => new Notification
        {
            UserId = map.UserId,
            Title = title,
            Message = message,
            Type = type,
            ActionPayload = payloadStr,
            SentDate = now,
            IsSent = false
        }).ToList();

        dbContext.Notifications.AddRange(notifications);
        await dbContext.SaveChangesAsync(cancellationToken);

        var validDonorIds = donorUserMap.Select(map => map.DonorId).ToList();
        var sent = await pushDispatcher.SendBatchAsync(validDonorIds, title, message, payload, cancellationToken);
        
        if (sent)
        {
            foreach (var notification in notifications)
            {
                notification.IsSent = true;
                notification.SentVia = "fcm";
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
