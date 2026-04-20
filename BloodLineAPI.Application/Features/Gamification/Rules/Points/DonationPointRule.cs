using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Extensions;
using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Points;

public sealed class DonationPointRule(IApplicationDbContext dbContext) : IPointRule
{
    public PointActionType ActionType => PointActionType.RegularDonation;

    public async Task<PointRuleResult?> EvaluateAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DonationCompleted || context.IsEmergencyDonation)
        {
            return null;
        }

        var donor = await dbContext.Donors.FindAsync([context.DonorId], cancellationToken);
        if (donor is null)
        {
            return null;
        }

        if (!donor.IsEligibleForRegularDonationAt(context.OccurredOn))
        {
            return null;
        }

        return new PointRuleResult(500, "Regular donation completed");
    }
}
