using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Badges.Action;

public sealed class NightOwlBadgeRule : IBadgeRule
{
    public string BadgeKey => "night_owl";

    public Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        var earned = context.Trigger == GamificationTrigger.DonationCompleted
            && context.OccurredOn.TimeOfDay < TimeSpan.FromHours(6);

        return Task.FromResult(earned);
    }
}
