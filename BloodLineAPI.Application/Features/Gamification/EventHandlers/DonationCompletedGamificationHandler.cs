using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Events;
using MediatR;

namespace BloodLineAPI.Application.Features.Gamification.EventHandlers;

public sealed class DonationCompletedGamificationHandler(IGamificationService gamificationService)
    : INotificationHandler<DonationCompletedEvent>
{
    public async Task Handle(DonationCompletedEvent notification, CancellationToken cancellationToken)
    {
        var context = new GamificationContext(
            notification.DonorId,
            GamificationTrigger.DonationCompleted,
            notification.OccurredOn,
            notification.DonationAppointmentId,
            notification.IsEmergencyDonation);

        await gamificationService.ProcessAsync(context, cancellationToken);
    }
}
