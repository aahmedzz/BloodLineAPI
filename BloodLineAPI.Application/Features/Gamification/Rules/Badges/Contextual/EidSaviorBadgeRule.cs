using System.Globalization;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Badges.Contextual;

public sealed class EidSaviorBadgeRule : IBadgeRule
{
    public string BadgeKey => "eid_savior";

    public Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DonationCompleted)
        {
            return Task.FromResult(false);
        }

        var calendar = new UmAlQuraCalendar();
        try
        {
            var month = calendar.GetMonth(context.OccurredOn);
            var day = calendar.GetDayOfMonth(context.OccurredOn);

            // Eid Al-Fitr: Shawwal (Month 10), days 1-3
            if (month == 10 && day >= 1 && day <= 3)
            {
                return Task.FromResult(true);
            }

            // Eid Al-Adha: Dhu al-Hijjah (Month 12), days 10-13
            if (month == 12 && day >= 10 && day <= 13)
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch
        {
            // Fallback in case of out of range dates for UmAlQuraCalendar
            return Task.FromResult(false);
        }
    }
}
