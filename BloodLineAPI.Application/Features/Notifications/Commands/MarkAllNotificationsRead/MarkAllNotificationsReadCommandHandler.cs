using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Notifications.Commands.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        return await dbContext.Notifications
            .Where(n => n.UserId == request.UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }
}
