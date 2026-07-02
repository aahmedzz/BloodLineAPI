using System;
using System.Collections.Generic;
using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IBackgroundNotificationService
{
    void EnqueueNotification(
        Guid donorId,
        string title,
        string message,
        NotificationType type,
        Dictionary<string, string>? payload = null);

    void EnqueueBatchNotification(
        IEnumerable<Guid> donorIds,
        string title,
        string message,
        NotificationType type,
        Dictionary<string, string>? payload = null);
}
