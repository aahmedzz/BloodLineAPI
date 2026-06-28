using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Points;

public sealed class ShareDailyInfoPointRule(IApplicationDbContext dbContext) : IPointRule
{
    public PointActionType ActionType => PointActionType.ShareDailyInfo;

    public async Task<PointRuleResult?> EvaluateAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DailyInfoShared)
        {
            return null;
        }

        var today = context.OccurredOn.Date;

        var alreadyShared = await dbContext.PointTransactions
            .AnyAsync(pt => pt.DonorId == context.DonorId &&
                            pt.ActionType == PointActionType.ShareDailyInfo &&
                            pt.TransactionDate.Date == today,
                      cancellationToken);

        if (alreadyShared)
        {
            return null;
        }

        return new PointRuleResult(50, "Daily information shared reward");
    }
}
