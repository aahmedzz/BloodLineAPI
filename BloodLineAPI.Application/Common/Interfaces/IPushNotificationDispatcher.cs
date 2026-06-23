namespace BloodLineAPI.Application.Common.Interfaces;

/// <summary>
/// Low-level infrastructure contract for dispatching raw push notifications to device(s).
/// This is an internal transport-layer abstraction used exclusively by <see cref="INotificationService"/>.
/// Application features should never depend on this directly.
/// </summary>
public interface IPushNotificationDispatcher
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
