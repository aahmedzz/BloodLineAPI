using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Badges.Specialized;

public sealed class TripleGiverBadgeRule(IApplicationDbContext dbContext) : IBadgeRule
{
    public string BadgeKey => "triple_giver";

    public async Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DonationCompleted || context.DonationAppointmentId == null)
        {
            return false;
        }

        var currentAppointment = await dbContext.DonationAppointments
            .FirstOrDefaultAsync(da => da.Id == context.DonationAppointmentId.Value, cancellationToken);

        if (currentAppointment == null)
        {
            return false;
        }

        var completedTypes = await dbContext.DonationAppointments
            .Where(da => da.DonorId == context.DonorId && da.Status == AppointmentStatus.Completed)
            .Select(da => da.DonationType)
            .Distinct()
            .ToListAsync(cancellationToken);

        var allTypes = completedTypes.ToHashSet();
        allTypes.Add(currentAppointment.DonationType);

        return allTypes.Contains(DonationType.WholeBlood) &&
               allTypes.Contains(DonationType.Platelets) &&
               allTypes.Contains(DonationType.Plasma);
    }
}
