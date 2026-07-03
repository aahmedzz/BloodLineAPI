using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface INotificationService
{
    Task<bool> SendNotificationAsync(
        Guid donorId,
        string title,
        string message,
        NotificationType type,
        Dictionary<string, string>? payload = null,
        CancellationToken cancellationToken = default);

    Task SendBatchNotificationAsync(
        IEnumerable<Guid> donorIds,
        string title,
        string message,
        NotificationType type,
        Dictionary<string, string>? payload = null,
        CancellationToken cancellationToken = default);
}
