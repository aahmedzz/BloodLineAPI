using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Events;
using MediatR;

namespace BloodLineAPI.Application.Features.Gamification.EventHandlers;

public sealed class DailyInfoReadGamificationHandler(IGamificationService gamificationService)
    : INotificationHandler<DailyInfoReadEvent>
{
    public async Task Handle(DailyInfoReadEvent notification, CancellationToken cancellationToken)
    {
        var context = new GamificationContext(
            notification.DonorId,
            GamificationTrigger.DailyInfoRead,
            notification.OccurredOn);

        await gamificationService.ProcessAsync(context, cancellationToken);
    }
}
