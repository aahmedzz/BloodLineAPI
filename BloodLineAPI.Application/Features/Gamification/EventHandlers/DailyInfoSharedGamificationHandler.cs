using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Events;
using MediatR;

namespace BloodLineAPI.Application.Features.Gamification.EventHandlers;

public sealed class DailyInfoSharedGamificationHandler(IGamificationService gamificationService)
    : INotificationHandler<DailyInfoSharedEvent>
{
    public async Task Handle(DailyInfoSharedEvent notification, CancellationToken cancellationToken)
    {
        var context = new GamificationContext(
            notification.DonorId,
            GamificationTrigger.DailyInfoShared,
            notification.OccurredOn);

        await gamificationService.ProcessAsync(context, cancellationToken);
    }
}
