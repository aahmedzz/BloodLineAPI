using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Badges.Action;

public sealed class AmbassadorBadgeRule(IApplicationDbContext dbContext) : IBadgeRule
{
    public string BadgeKey => "ambassador";

    public async Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.RequestShared)
        {
            return false;
        }

        var totalShares = await dbContext.PointTransactions
            .CountAsync(pt => pt.DonorId == context.DonorId && pt.ActionType == PointActionType.ShareRequest, cancellationToken);

        return totalShares + 1 >= 20;
    }
}
