using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class SendNotificationBackgroundJob(
    INotificationService notificationService,
    ILogger<SendNotificationBackgroundJob> logger)
{
    public async Task SendAsync(
        Guid donorId,
        string title,
        string message,
        NotificationType type,
        Dictionary<string, string>? payload,
        CancellationToken ct = default)
    {
        try
        {
            await notificationService.SendNotificationAsync(donorId, title, message, type, payload, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send background notification to donor {DonorId}.", donorId);
            throw; // Rethrow to allow Hangfire automatic retries
        }
    }

    public async Task SendBatchAsync(
        List<Guid> donorIds,
        string title,
        string message,
        NotificationType type,
        Dictionary<string, string>? payload,
        CancellationToken ct = default)
    {
        try
        {
            await notificationService.SendBatchNotificationAsync(donorIds, title, message, type, payload, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send background batch notification to {Count} donors.", donorIds.Count);
            throw; // Rethrow to allow Hangfire automatic retries
        }
    }
}
