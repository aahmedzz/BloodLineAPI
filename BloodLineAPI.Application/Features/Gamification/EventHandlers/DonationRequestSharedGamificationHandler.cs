using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Events;
using MediatR;

namespace BloodLineAPI.Application.Features.Gamification.EventHandlers;

public sealed class DonationRequestSharedGamificationHandler(IGamificationService gamificationService)
    : INotificationHandler<DonationRequestSharedEvent>
{
    public async Task Handle(DonationRequestSharedEvent notification, CancellationToken cancellationToken)
    {
        var context = new GamificationContext(
            notification.DonorId,
            GamificationTrigger.RequestShared,
            notification.OccurredOn,
            UrgentBloodAppealId: notification.UrgentBloodAppealId);

        await gamificationService.ProcessAsync(context, cancellationToken);
    }
}
