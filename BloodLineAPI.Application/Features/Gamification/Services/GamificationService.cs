using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Services;

public sealed class GamificationService(
    IEnumerable<IPointRule> pointRules,
    IEnumerable<IBadgeRule> badgeRules,
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : IGamificationService
{
    public async Task ProcessAsync(GamificationContext context, CancellationToken cancellationToken)
    {
        var donor = await dbContext.Donors
            .Include(d => d.BloodType)
            .Include(d => d.DonorBadges)
            .FirstOrDefaultAsync(d => d.Id == context.DonorId, cancellationToken);

        if (donor is null)
        {
            return;
        }

        var localOccurredOn = dateTimeProvider.ToLocalTime(context.OccurredOn);
        var localContext = context with { OccurredOn = localOccurredOn };
        var monthKey = localOccurredOn.ToString("yyyy-MM");
        var currentMonthKey = dateTimeProvider.LocalNow.ToString("yyyy-MM");
        var totalAwardedPoints = 0;
        var monthlyAwardedPoints = 0;

        foreach (var pointRule in pointRules)
        {
            var result = await pointRule.EvaluateAsync(localContext, cancellationToken);
            if (result is null || result.Points <= 0)
            {
                continue;
            }

            dbContext.PointTransactions.Add(new PointTransaction
            {
                DonorId = donor.Id,
                ActionType = pointRule.ActionType,
                Points = result.Points,
                Description = result.Description,
                MonthKey = monthKey,
                TransactionDate = context.OccurredOn
            });

            totalAwardedPoints += result.Points;
            if (monthKey == currentMonthKey)
            {
                monthlyAwardedPoints += result.Points;
            }
        }

        var badgesByKey = await dbContext.Badges.ToDictionaryAsync(b => b.BadgeKey, cancellationToken);
        var earnedBadgeIds = donor.DonorBadges.Select(db => db.BadgeId).ToHashSet();

        foreach (var badgeRule in badgeRules)
        {
            if (!badgesByKey.TryGetValue(badgeRule.BadgeKey, out var badge) || earnedBadgeIds.Contains(badge.Id))
            {
                continue;
            }

            var earned = await badgeRule.IsEarnedAsync(localContext, cancellationToken);
            if (!earned)
            {
                continue;
            }

            dbContext.DonorBadges.Add(new DonorBadge
            {
                DonorId = donor.Id,
                BadgeId = badge.Id,
                EarnedDate = context.OccurredOn
            });

            if (badge.BonusPoints > 0)
            {
                dbContext.PointTransactions.Add(new PointTransaction
                {
                    DonorId = donor.Id,
                    ActionType = PointActionType.BadgeBonus,
                    Points = badge.BonusPoints,
                    Description = $"Badge bonus: {badge.BadgeName}",
                    MonthKey = monthKey,
                    TransactionDate = context.OccurredOn
                });

                totalAwardedPoints += badge.BonusPoints;
                if (monthKey == currentMonthKey)
                {
                    monthlyAwardedPoints += badge.BonusPoints;
                }
            }

            earnedBadgeIds.Add(badge.Id);
        }

        if (context.Trigger == GamificationTrigger.DonationCompleted)
        {
            donor.TotalDonationCount += 1;
            donor.LastDonationDate = context.OccurredOn;
        }

        donor.TotalPoints += totalAwardedPoints;
        donor.MonthlyPoints += monthlyAwardedPoints;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
