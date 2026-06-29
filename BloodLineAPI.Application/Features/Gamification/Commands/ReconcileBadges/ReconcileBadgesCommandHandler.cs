using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Gamification.Commands.ReconcileBadges;

public sealed class ReconcileBadgesCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ReconcileBadgesCommand, Result<ReconcileBadgesResultDto>>
{
    public async Task<Result<ReconcileBadgesResultDto>> Handle(ReconcileBadgesCommand request, CancellationToken cancellationToken)
    {
        var allBadges = await dbContext.Badges
            .ToListAsync(cancellationToken);

        var badgesByKey = allBadges
            .ToDictionary(b => b.BadgeKey.ToLowerInvariant(), b => b);

        var donors = await dbContext.Donors
            .Include(d => d.DonorBadges)
            .ToListAsync(cancellationToken);

        var completedAppointments = await dbContext.DonationAppointments
            .Include(da => da.DonationCenter)
            .Where(da => da.Status == AppointmentStatus.Completed)
            .ToListAsync(cancellationToken);

        var appointmentsByDonor = completedAppointments
            .GroupBy(da => da.DonorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var currentMonthKey = dateTimeProvider.LocalNow.ToString("yyyy-MM");

        int totalDonorsChecked = donors.Count;
        int totalDonorsUpdated = 0;
        int totalBadgesAwarded = 0;
        int totalPointsAwarded = 0;
        var details = new List<DonorReconciliationDetailDto>();

        foreach (var donor in donors)
        {
            if (!appointmentsByDonor.TryGetValue(donor.Id, out var donorAppointments) || donorAppointments.Count == 0)
            {
                continue;
            }

            var earnedBadgeIds = donor.DonorBadges.Select(db => db.BadgeId).ToHashSet();
            var chronologicalAppointments = donorAppointments.OrderBy(da => da.ScheduledDate).ToList();

            var completedTypes = new HashSet<DonationType>();
            int milestoneCount = 0;
            int donorBadgesAwarded = 0;
            int donorPointsAwarded = 0;
            var awardedBadgeKeys = new List<string>();

            foreach (var appointment in chronologicalAppointments)
            {
                milestoneCount++;
                completedTypes.Add(appointment.DonationType);

                // Earned date is appointment date + start time
                var earnedDate = appointment.ScheduledDate.Add(appointment.StartTime);

                // We evaluate which badges are earned at this point in time
                var eligibleBadgeKeys = new List<string>();

                // Milestone rules
                if (milestoneCount >= 1) eligibleBadgeKeys.Add("giver");
                if (milestoneCount >= 3) eligibleBadgeKeys.Add("helper");
                if (milestoneCount >= 5) eligibleBadgeKeys.Add("hero");
                if (milestoneCount >= 10) eligibleBadgeKeys.Add("life_saver");
                if (milestoneCount >= 11) eligibleBadgeKeys.Add("monqez");

                // Specialized rules
                if (appointment.DonationType == DonationType.Plasma) eligibleBadgeKeys.Add("yellow_gold");
                if (appointment.DonationType == DonationType.Platelets) eligibleBadgeKeys.Add("platelet_guardian");

                if (completedTypes.Contains(DonationType.WholeBlood) &&
                    completedTypes.Contains(DonationType.Platelets) &&
                    completedTypes.Contains(DonationType.Plasma))
                {
                    eligibleBadgeKeys.Add("triple_giver");
                }

                // Contextual rules
                if (!string.IsNullOrEmpty(donor.District) &&
                    appointment.DonationCenter != null &&
                    !string.IsNullOrEmpty(appointment.DonationCenter.Location) &&
                    !donor.District.Equals(appointment.DonationCenter.Location, StringComparison.OrdinalIgnoreCase))
                {
                    eligibleBadgeKeys.Add("traveler_lifesaver");
                }

                if (appointment.ScheduledDate.DayOfWeek == DayOfWeek.Friday ||
                    appointment.ScheduledDate.DayOfWeek == DayOfWeek.Saturday)
                {
                    eligibleBadgeKeys.Add("weekend_hero");
                }

                if (IsRamadan(appointment.ScheduledDate))
                {
                    eligibleBadgeKeys.Add("ramadan_light");
                }

                if (IsEid(appointment.ScheduledDate))
                {
                    eligibleBadgeKeys.Add("eid_savior");
                }

                var month = appointment.ScheduledDate.Month;
                if (month == 12 || month == 1 || month == 2)
                {
                    eligibleBadgeKeys.Add("winter_guard");
                }

                // Award the eligible badges
                foreach (var badgeKey in eligibleBadgeKeys)
                {
                    if (badgesByKey.TryGetValue(badgeKey, out var badge))
                    {
                        if (!earnedBadgeIds.Contains(badge.Id))
                        {
                            // Add DonorBadge record
                            var donorBadge = new DonorBadge
                            {
                                Id = Guid.NewGuid(),
                                DonorId = donor.Id,
                                BadgeId = badge.Id,
                                EarnedDate = earnedDate
                            };

                            dbContext.DonorBadges.Add(donorBadge);
                            earnedBadgeIds.Add(badge.Id);
                            awardedBadgeKeys.Add(badgeKey);
                            donorBadgesAwarded++;

                            // Award Bonus Points
                            if (badge.BonusPoints > 0)
                            {
                                var monthKey = earnedDate.ToString("yyyy-MM");
                                dbContext.PointTransactions.Add(new PointTransaction
                                {
                                    Id = Guid.NewGuid(),
                                    DonorId = donor.Id,
                                    ActionType = PointActionType.BadgeBonus,
                                    Points = badge.BonusPoints,
                                    Description = $"Badge bonus: {badge.BadgeName}",
                                    MonthKey = monthKey,
                                    TransactionDate = earnedDate
                                });

                                donor.TotalPoints += badge.BonusPoints;
                                if (monthKey == currentMonthKey)
                                {
                                    donor.MonthlyPoints += badge.BonusPoints;
                                }

                                donorPointsAwarded += badge.BonusPoints;
                            }
                        }
                    }
                }
            }

            if (donorBadgesAwarded > 0)
            {
                totalDonorsUpdated++;
                totalBadgesAwarded += donorBadgesAwarded;
                totalPointsAwarded += donorPointsAwarded;

                details.Add(new DonorReconciliationDetailDto(
                    donor.Id,
                    donor.FullName,
                    donorBadgesAwarded,
                    donorPointsAwarded,
                    awardedBadgeKeys));
            }
        }

        if (totalDonorsUpdated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<ReconcileBadgesResultDto>.Success(new ReconcileBadgesResultDto(
            totalDonorsChecked,
            totalDonorsUpdated,
            totalBadgesAwarded,
            totalPointsAwarded,
            details));
    }

    private static bool IsRamadan(DateTime date)
    {
        var calendar = new UmAlQuraCalendar();
        try
        {
            var hijriMonth = calendar.GetMonth(date);
            return hijriMonth == 9;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsEid(DateTime date)
    {
        var calendar = new UmAlQuraCalendar();
        try
        {
            var month = calendar.GetMonth(date);
            var day = calendar.GetDayOfMonth(date);

            // Eid Al-Fitr: Shawwal (Month 10), days 1-3
            if (month == 10 && day >= 1 && day <= 3)
            {
                return true;
            }

            // Eid Al-Adha: Dhu al-Hijjah (Month 12), days 10-13
            if (month == 12 && day >= 10 && day <= 13)
            {
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
