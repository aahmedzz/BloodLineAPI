using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Gamification.Services;

public sealed class GamificationService(
    IEnumerable<IPointRule> pointRules,
    IEnumerable<IBadgeRule> badgeRules,
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    INotificationService notificationService) : IGamificationService
{
    private static readonly Dictionary<PointActionType, string> ArabicActionNames = new()
    {
        { PointActionType.DownloadApp, "تحميل وتفعيل التطبيق" },
        { PointActionType.WholeBloodDonation, "تبرع بالدم الكامل" },
        { PointActionType.PlateletPlasmaDonation, "تبرع بالصفائح الدموية أو البلازما" },
        { PointActionType.EmergencyResponse, "استجابة لطلب تبرع طارئ" },
        { PointActionType.ReadDailyInfo, "قراءة النصيحة اليومية" },
        { PointActionType.ShareDailyInfo, "مشاركة النصيحة اليومية بنجاح" },
        { PointActionType.BadgeBonus, "مكافأة الشارة" }
    };

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

        var pointsNotificationsToSend = new List<(int Points, string ActionArabicName)>();

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
                TransactionDate = localOccurredOn
            });

            totalAwardedPoints += result.Points;
            if (monthKey == currentMonthKey)
            {
                monthlyAwardedPoints += result.Points;
            }

            var actionArabicName = ArabicActionNames.TryGetValue(pointRule.ActionType, out var name)
                ? name
                : result.Description;
            pointsNotificationsToSend.Add((result.Points, actionArabicName));
        }

        var badgesByKey = await dbContext.Badges.ToDictionaryAsync(b => b.BadgeKey, cancellationToken);
        var earnedBadgeIds = donor.DonorBadges.Select(db => db.BadgeId).ToHashSet();
        var badgesNotificationsToSend = new List<Badge>();

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
                EarnedDate = localOccurredOn
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
                    TransactionDate = localOccurredOn
                });

                totalAwardedPoints += badge.BonusPoints;
                if (monthKey == currentMonthKey)
                {
                    monthlyAwardedPoints += badge.BonusPoints;
                }
            }

            earnedBadgeIds.Add(badge.Id);
            badgesNotificationsToSend.Add(badge);
        }

        if (context.Trigger == GamificationTrigger.DonationCompleted)
        {
            donor.TotalDonationCount += 1;
            donor.LastDonationDate = localOccurredOn;
        }

        donor.TotalPoints += totalAwardedPoints;
        donor.MonthlyPoints += monthlyAwardedPoints;

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var (points, actionName) in pointsNotificationsToSend)
        {
            var title = $"🎉 نقاط جديدة! (+{points} XP)";
            var message = $"لقد حصلت على +{points} نقطة مقابل: {actionName}";

            await notificationService.SendNotificationAsync(
                donor.Id,
                title,
                message,
                NotificationType.PointsEarned,
                new Dictionary<string, string> { { "pointsAwarded", points.ToString() } },
                cancellationToken);
        }

        foreach (var badge in badgesNotificationsToSend)
        {
            var badgeName = !string.IsNullOrEmpty(badge.BadgeNameAr) ? badge.BadgeNameAr : badge.BadgeName;
            var title = badge.BonusPoints > 0 
                ? $"🏆 شارة جديدة! (+{badge.BonusPoints} XP)"
                : "🏆 شارة جديدة!";
            var message = $"تهانينا! لقد حصلت على شارة '{badgeName}'";

            await notificationService.SendNotificationAsync(
                donor.Id,
                title,
                message,
                NotificationType.BadgeEarned,
                new Dictionary<string, string> { { "badgeKey", badge.BadgeKey } },
                cancellationToken);
        }
    }
}
