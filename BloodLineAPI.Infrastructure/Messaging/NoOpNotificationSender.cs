using BloodLineAPI.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.Messaging;

public class NoOpNotificationSender(ILogger<NoOpNotificationSender> logger) : IPushNotificationDispatcher
{
    public Task<bool> SendAsync(Guid donorId, string title, string message, CancellationToken ct = default)
    {
        logger.LogInformation("Notification queued for donor {DonorId}: {Title} - {Message}", donorId, title, message);
        return Task.FromResult(true);
    }

    public Task<bool> SendAsync(Guid donorId, string title, string message, Dictionary<string, string>? data, CancellationToken ct = default)
    {
        logger.LogInformation("Notification queued for donor {DonorId} with data: {Title} - {Message}", donorId, title, message);
        return Task.FromResult(true);
    }

    public Task<bool> SendBatchAsync(IEnumerable<Guid> donorIds, string title, string message, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        logger.LogInformation("Batch notification queued for donors: {Title} - {Message}", title, message);
        return Task.FromResult(true);
    }
}
