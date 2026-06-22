using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Notifications.Commands;

public sealed record SendTestNotificationCommand(Guid UserId, string Title, string Message) : IRequest;

public sealed class SendTestNotificationCommandHandler(
    INotificationService notificationService,
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

        await notificationService.SendNotificationAsync(
            donorId,
            request.Title,
            request.Message,
            NotificationType.General,
            payload: null,
            cancellationToken);
    }
}