using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Points;

public sealed class PlateletPlasmaPointRule(IApplicationDbContext dbContext) : IPointRule
{
    public PointActionType ActionType => PointActionType.PlateletPlasmaDonation;

    public async Task<PointRuleResult?> EvaluateAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DonationCompleted || context.DonationAppointmentId == null)
        {
            return null;
        }

        var appointment = await dbContext.DonationAppointments
            .FirstOrDefaultAsync(da => da.Id == context.DonationAppointmentId.Value, cancellationToken);

        if (appointment == null || (appointment.DonationType != DonationType.Platelets && appointment.DonationType != DonationType.Plasma) || appointment.UrgentBloodAppealId != null)
        {
            return null;
        }

        var typeStr = appointment.DonationType == DonationType.Platelets ? "Platelet" : "Plasma";
        return new PointRuleResult(700, $"{typeStr} donation completed");
    }
}
