using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Badges.Action;

public sealed class ResponderBadgeRule : IBadgeRule
{
    public string BadgeKey => "responder";

    public Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        var earned = context.Trigger == GamificationTrigger.DonationCompleted && context.IsEmergencyDonation;
        return Task.FromResult(earned);
    }
}
