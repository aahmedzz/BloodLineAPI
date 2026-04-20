using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Badges.Action;

public sealed class GoldenBloodBadgeRule(IApplicationDbContext dbContext) : IBadgeRule
{
    public string BadgeKey => "golden_blood";

    public async Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        var donor = await dbContext.Donors
            .AsNoTracking()
            .Include(d => d.BloodType)
            .FirstOrDefaultAsync(d => d.Id == context.DonorId, cancellationToken);

        if (donor?.BloodType is null)
        {
            return false;
        }

        var isRareGroup = donor.BloodType.BloodGroupName is BloodGroupName.O or BloodGroupName.AB;
        return isRareGroup && donor.BloodType.RhFactor == RhFactor.Negative;
    }
}
