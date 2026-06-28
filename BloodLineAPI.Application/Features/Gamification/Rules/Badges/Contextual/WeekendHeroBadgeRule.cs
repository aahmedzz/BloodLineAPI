using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Badges.Contextual;

public sealed class WeekendHeroBadgeRule : IBadgeRule
{
    public string BadgeKey => "weekend_hero";

    public Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DonationCompleted)
        {
            return Task.FromResult(false);
        }

        var isWeekend = context.OccurredOn.DayOfWeek == DayOfWeek.Friday ||
                        context.OccurredOn.DayOfWeek == DayOfWeek.Saturday;

        return Task.FromResult(isWeekend);
    }
}
