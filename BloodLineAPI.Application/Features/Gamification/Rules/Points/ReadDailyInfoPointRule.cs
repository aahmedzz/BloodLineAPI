using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Points;

public sealed class ReadDailyInfoPointRule(IApplicationDbContext dbContext) : IPointRule
{
    public PointActionType ActionType => PointActionType.ReadDailyInfo;

    public async Task<PointRuleResult?> EvaluateAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.DailyInfoRead)
        {
            return null;
        }

        var today = context.OccurredOn.Date;

        var alreadyRead = await dbContext.PointTransactions
            .AnyAsync(pt => pt.DonorId == context.DonorId &&
                            pt.ActionType == PointActionType.ReadDailyInfo &&
                            pt.TransactionDate.Date == today,
                      cancellationToken);

        if (alreadyRead)
        {
            return null;
        }

        return new PointRuleResult(20, "Daily information read reward");
    }
}
