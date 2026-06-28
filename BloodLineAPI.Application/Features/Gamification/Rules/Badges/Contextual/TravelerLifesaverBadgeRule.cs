using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Badges.Contextual;

public sealed class TravelerLifesaverBadgeRule(IApplicationDbContext dbContext) : IBadgeRule
{
    public string BadgeKey => "traveler_lifesaver";

    public async Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DonationCompleted || context.DonationAppointmentId == null)
        {
            return false;
        }

        var appointment = await dbContext.DonationAppointments
            .Include(da => da.DonationCenter)
            .FirstOrDefaultAsync(da => da.Id == context.DonationAppointmentId.Value, cancellationToken);

        if (appointment == null)
        {
            return false;
        }

        var donor = await dbContext.Donors.FindAsync([context.DonorId], cancellationToken);
        if (donor == null)
        {
            return false;
        }

        return !string.IsNullOrEmpty(donor.District) &&
               !string.IsNullOrEmpty(appointment.DonationCenter.Location) &&
               !donor.District.Equals(appointment.DonationCenter.Location, StringComparison.OrdinalIgnoreCase);
    }
}
