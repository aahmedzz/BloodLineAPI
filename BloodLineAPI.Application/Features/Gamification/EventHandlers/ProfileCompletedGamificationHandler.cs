using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Events;
using MediatR;

namespace BloodLineAPI.Application.Features.Gamification.EventHandlers;

public sealed class ProfileCompletedGamificationHandler(IGamificationService gamificationService)
    : INotificationHandler<ProfileCompletedEvent>
{
    public async Task Handle(ProfileCompletedEvent notification, CancellationToken cancellationToken)
    {
        var context = new GamificationContext(
            notification.DonorId,
            GamificationTrigger.ProfileCompleted,
            notification.OccurredOn);

        await gamificationService.ProcessAsync(context, cancellationToken);
    }
}
