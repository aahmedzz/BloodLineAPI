using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Points;

public sealed class EmergencyDonationPointRule : IPointRule
{
    public PointActionType ActionType => PointActionType.EmergencyDonation;

    public Task<PointRuleResult?> EvaluateAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DonationCompleted || !context.IsEmergencyDonation)
        {
            return Task.FromResult<PointRuleResult?>(null);
        }

        return Task.FromResult<PointRuleResult?>(new PointRuleResult(800, "Emergency donation completed"));
    }
}
