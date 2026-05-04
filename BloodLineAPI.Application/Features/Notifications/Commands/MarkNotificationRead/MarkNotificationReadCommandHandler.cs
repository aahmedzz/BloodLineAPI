using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException("Notification", request.NotificationId);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
