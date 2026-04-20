using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Points;

public sealed class ShareRequestPointRule(IApplicationDbContext dbContext) : IPointRule
{
    public PointActionType ActionType => PointActionType.ShareRequest;

    public async Task<PointRuleResult?> EvaluateAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.RequestShared)
        {
            return null;
        }

        var dayStart = context.OccurredOn.Date;
        var dayEnd = dayStart.AddDays(1);

        var sharesToday = await dbContext.PointTransactions
            .CountAsync(pt =>
                pt.DonorId == context.DonorId &&
                pt.ActionType == PointActionType.ShareRequest &&
                pt.TransactionDate >= dayStart &&
                pt.TransactionDate < dayEnd,
                cancellationToken);

        if (sharesToday >= 5)
        {
            return null;
        }

        return new PointRuleResult(10, "Urgent request shared");
    }
}
