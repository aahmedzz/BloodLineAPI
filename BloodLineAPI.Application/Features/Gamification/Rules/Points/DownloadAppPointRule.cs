using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Points;

public sealed class DownloadAppPointRule(IApplicationDbContext dbContext) : IPointRule
{
    public PointActionType ActionType => PointActionType.DownloadApp;

    public async Task<PointRuleResult?> EvaluateAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.ProfileCompleted)
        {
            return null;
        }

        var alreadyAwarded = await dbContext.PointTransactions
            .AnyAsync(pt => pt.DonorId == context.DonorId && pt.ActionType == PointActionType.DownloadApp, cancellationToken);

        if (alreadyAwarded)
        {
            return null;
        }

        return new PointRuleResult(100, "Download and activate app reward");
    }
}
