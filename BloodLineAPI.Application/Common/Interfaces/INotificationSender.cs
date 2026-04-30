namespace BloodLineAPI.Application.Common.Interfaces;

public interface INotificationSender
{
    /// <summary>Send a push notification to a single donor.</summary>
    Task<bool> SendAsync(Guid donorId, string title, string message, CancellationToken ct = default);

    /// <summary>Send a push notification with a data payload for in-app routing.</summary>
    Task<bool> SendAsync(Guid donorId, string title, string message,
                   Dictionary<string, string>? data, CancellationToken ct = default);

    /// <summary>Send a push notification to multiple donors.</summary>
    Task<bool> SendBatchAsync(IEnumerable<Guid> donorIds, string title, string message,
                        Dictionary<string, string>? data = null, CancellationToken ct = default);
}
