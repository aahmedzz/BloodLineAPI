using BloodLineAPI.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.Messaging;

public class NoOpNotificationSender(ILogger<NoOpNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(Guid donorId, string title, string message, CancellationToken ct = default)
    {
        logger.LogInformation("Notification queued for donor {DonorId}: {Title} - {Message}", donorId, title, message);
        return Task.CompletedTask;
    }
}
