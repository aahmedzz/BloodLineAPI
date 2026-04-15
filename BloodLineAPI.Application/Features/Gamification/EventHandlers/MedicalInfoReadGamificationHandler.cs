using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Events;
using MediatR;

namespace BloodLineAPI.Application.Features.Gamification.EventHandlers;

public sealed class MedicalInfoReadGamificationHandler(IGamificationService gamificationService)
    : INotificationHandler<MedicalInfoReadEvent>
{
    public async Task Handle(MedicalInfoReadEvent notification, CancellationToken cancellationToken)
    {
        var context = new GamificationContext(
            notification.DonorId,
            GamificationTrigger.MedicalInfoRead,
            notification.OccurredOn,
            MedicalInfoId: notification.MedicalInfoId);

        await gamificationService.ProcessAsync(context, cancellationToken);
    }
}
