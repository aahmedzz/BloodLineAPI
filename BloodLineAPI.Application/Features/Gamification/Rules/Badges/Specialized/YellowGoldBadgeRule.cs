using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Badges.Specialized;

public sealed class YellowGoldBadgeRule(IApplicationDbContext dbContext) : IBadgeRule
{
    public string BadgeKey => "yellow_gold";

    public async Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DonationCompleted || context.DonationAppointmentId == null)
        {
            return false;
        }

        var appointment = await dbContext.DonationAppointments
            .FirstOrDefaultAsync(da => da.Id == context.DonationAppointmentId.Value, cancellationToken);

        return appointment != null && appointment.DonationType == DonationType.Plasma;
    }
}
