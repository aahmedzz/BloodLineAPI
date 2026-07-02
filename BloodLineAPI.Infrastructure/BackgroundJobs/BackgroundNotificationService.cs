using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using Hangfire;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class BackgroundNotificationService(IBackgroundJobClient backgroundJobClient)
    : IBackgroundNotificationService
{
    public void EnqueueNotification(
        Guid donorId,
        string title,
        string message,
        NotificationType type,
        Dictionary<string, string>? payload = null)
    {
        backgroundJobClient.Enqueue<SendNotificationBackgroundJob>(
            job => job.SendAsync(donorId, title, message, type, payload, CancellationToken.None));
    }

    public void EnqueueBatchNotification(
        IEnumerable<Guid> donorIds,
        string title,
        string message,
        NotificationType type,
        Dictionary<string, string>? payload = null)
    {
        var idsList = donorIds.ToList();
        backgroundJobClient.Enqueue<SendNotificationBackgroundJob>(
            job => job.SendBatchAsync(idsList, title, message, type, payload, CancellationToken.None));
    }
}
