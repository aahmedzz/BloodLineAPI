using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Badges.Milestone;

public sealed class HelperBadgeRule(IApplicationDbContext dbContext) : IBadgeRule
{
    public string BadgeKey => "helper";

    public async Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DonationCompleted)
        {
            return false;
        }

        var donor = await dbContext.Donors.FindAsync([context.DonorId], cancellationToken);
        if (donor is null)
        {
            return false;
        }

        return donor.TotalDonationCount + 1 >= 3;
    }
}
