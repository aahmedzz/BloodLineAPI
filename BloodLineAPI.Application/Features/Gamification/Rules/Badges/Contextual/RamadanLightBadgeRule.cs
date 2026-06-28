using System.Globalization;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Badges.Contextual;

public sealed class RamadanLightBadgeRule : IBadgeRule
{
    public string BadgeKey => "ramadan_light";

    public Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DonationCompleted)
        {
            return Task.FromResult(false);
        }

        var calendar = new UmAlQuraCalendar();
        try
        {
            var hijriMonth = calendar.GetMonth(context.OccurredOn);
            return Task.FromResult(hijriMonth == 9);
        }
        catch
        {
            // Fallback in case of out of range dates for UmAlQuraCalendar
            return Task.FromResult(false);
        }
    }
}
