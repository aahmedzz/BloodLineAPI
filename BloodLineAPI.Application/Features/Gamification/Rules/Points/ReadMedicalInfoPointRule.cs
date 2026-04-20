using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Rules.Points;

public sealed class ReadMedicalInfoPointRule(IApplicationDbContext dbContext) : IPointRule
{
    public PointActionType ActionType => PointActionType.ReadMedicalInfo;

    public async Task<PointRuleResult?> EvaluateAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        if (context.Trigger != GamificationTrigger.MedicalInfoRead)
        {
            return null;
        }

        var daysFromMonday = ((int)context.OccurredOn.DayOfWeek + 6) % 7;
        var weekStart = context.OccurredOn.Date.AddDays(-daysFromMonday);
        var nextWeek = weekStart.AddDays(7);

        var readsThisWeek = await dbContext.PointTransactions
            .CountAsync(pt =>
                pt.DonorId == context.DonorId &&
                pt.ActionType == PointActionType.ReadMedicalInfo &&
                pt.TransactionDate >= weekStart &&
                pt.TransactionDate < nextWeek,
                cancellationToken);

        if (readsThisWeek >= 10)
        {
            return null;
        }

        return new PointRuleResult(5, "Medical info article read");
    }
}
