using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Badges.Contextual;

public sealed class WinterGuardBadgeRule : IBadgeRule
{
    public string BadgeKey => "winter_guard";

    public Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DonationCompleted)
        {
            return Task.FromResult(false);
        }

        var month = context.OccurredOn.Month;
        var isWinter = month == 12 || month == 1 || month == 2;

        return Task.FromResult(isWinter);
    }
}
