using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Points;

public sealed class EmergencyResponsePointRule(IApplicationDbContext dbContext) : IPointRule
{
    public PointActionType ActionType => PointActionType.EmergencyResponse;

    public async Task<PointRuleResult?> EvaluateAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DonationCompleted || context.DonationAppointmentId == null)
        {
            return null;
        }

        var appointment = await dbContext.DonationAppointments
            .FirstOrDefaultAsync(da => da.Id == context.DonationAppointmentId.Value, cancellationToken);

        if (appointment == null || appointment.UrgentBloodAppealId == null)
        {
            return null;
        }

        return new PointRuleResult(800, "Emergency response donation completed");
    }
}
