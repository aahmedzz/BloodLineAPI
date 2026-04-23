namespace BloodLineAPI.Application.Common.Interfaces;

public interface INotificationSender
{
    Task SendAsync(Guid donorId, string title, string message, CancellationToken ct = default);
}
