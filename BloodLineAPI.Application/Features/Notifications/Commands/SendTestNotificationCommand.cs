using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Notifications.Commands;

public sealed record SendTestNotificationCommand(Guid UserId, string Title, string Message) : IRequest;

public sealed class SendTestNotificationCommandHandler(
    INotificationSender notificationSender,
    IApplicationDbContext dbContext) : IRequestHandler<SendTestNotificationCommand>
{
    public async Task Handle(SendTestNotificationCommand request, CancellationToken cancellationToken)
    {
        // Get the donor ID associated with the user
        var donorId = await dbContext.Donors
            .Where(d => d.User.Id == request.UserId)
            .Select(d => d.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (donorId == Guid.Empty)
            return;

        // 1. Persist the notification record
        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = "test",
            Priority = 0,
            SentDate = DateTime.UtcNow,
            IsSent = false
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 2. Send push notification
        var sent = await notificationSender.SendAsync(donorId, request.Title, request.Message, cancellationToken);

        // 3. Update delivery status
        if (sent)
        {
            notification.IsSent = true;
            notification.SentVia = "fcm";
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}