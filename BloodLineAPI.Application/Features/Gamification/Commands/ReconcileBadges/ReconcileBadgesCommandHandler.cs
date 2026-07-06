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
        try
        {
            var allBadges = await dbContext.Badges
            .ToListAsync(cancellationToken);

        var badgesByKey = allBadges
            .ToDictionary(b => b.BadgeKey.ToLowerInvariant(), b => b);

        var badgeIdToBadge = allBadges
            .ToDictionary(b => b.Id, b => b);

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

        var allowedActionTypes = new[]
        {
            PointActionType.WholeBloodDonation,
            PointActionType.PlateletPlasmaDonation,
            PointActionType.EmergencyResponse,
            PointActionType.BadgeBonus
        };

        var allPointTransactions = await dbContext.PointTransactions
            .Where(pt => allowedActionTypes.Contains(pt.ActionType))
            .ToListAsync(cancellationToken);

        var pointTransactionsByDonor = allPointTransactions
            .GroupBy(pt => pt.DonorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var currentMonthKey = dateTimeProvider.LocalNow.ToString("yyyy-MM");

        int totalDonorsChecked = donors.Count;
        int totalDonorsUpdated = 0;
        int totalDonationCountsCorrected = 0;
        int totalBadgesAwarded = 0;
        int totalBadgesRemoved = 0;
        int totalPointsAwarded = 0;
        int totalPointsDeducted = 0;
        int totalDonationPointsAwarded = 0;
        var details = new List<DonorReconciliationDetailDto>();

        foreach (var donor in donors)
        {
            var donorAppointments = appointmentsByDonor.TryGetValue(donor.Id, out var apps) 
                ? apps 
                : new List<DonationAppointment>();

            var donorTransactions = pointTransactionsByDonor.TryGetValue(donor.Id, out var txs) 
                ? txs 
                : new List<PointTransaction>();

            int previousDonationCount = donor.TotalDonationCount;
            int correctedDonationCount = donorAppointments.Count;
            bool isDonationCountCorrected = (previousDonationCount != correctedDonationCount);

            if (isDonationCountCorrected)
            {
                donor.TotalDonationCount = correctedDonationCount;
                totalDonationCountsCorrected++;
            }

            var chronologicalAppointments = donorAppointments.OrderBy(da => da.ScheduledDate).ToList();

            var completedTypes = new HashSet<DonationType>();
            int milestoneCount = 0;
            var qualifiedBadgeKeys = new HashSet<string>();
            var earnedBadgeDates = new Dictionary<string, DateTime>();

            int badgesAwardedCount = 0;
            int badgesRemovedCount = 0;
            int pointsAwardedCount = 0;
            int pointsDeductedCount = 0;
            int donationPointsAwardedCount = 0;
            var awardedBadgeKeys = new List<string>();
            var removedBadgeKeys = new List<string>();

            // 1. Process chronological donations for milestone, specialized, and contextual badges
            foreach (var appointment in chronologicalAppointments)
            {
                milestoneCount++;
                completedTypes.Add(appointment.DonationType);

                // Earned date is appointment date + start time
                var earnedDate = appointment.ScheduledDate.Add(appointment.StartTime);

                // Determine badges earned at this point in time
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

                // Collect all qualified badge keys with their earliest earned date
                foreach (var badgeKey in eligibleBadgeKeys)
                {
                    if (!qualifiedBadgeKeys.Contains(badgeKey))
                    {
                        qualifiedBadgeKeys.Add(badgeKey);
                        earnedBadgeDates[badgeKey] = earnedDate;
                    }
                }

                // 2. Reconcile points for the donation itself
                PointActionType expectedActionType;
                int expectedPoints;
                string expectedDescription;

                if (appointment.UrgentBloodAppealId != null)
                {
                    expectedActionType = PointActionType.EmergencyResponse;
                    expectedPoints = 800;
                    expectedDescription = "Emergency response donation completed";
                }
                else if (appointment.DonationType == DonationType.WholeBlood)
                {
                    expectedActionType = PointActionType.WholeBloodDonation;
                    expectedPoints = 500;
                    expectedDescription = "Whole blood donation completed";
                }
                else
                {
                    expectedActionType = PointActionType.PlateletPlasmaDonation;
                    expectedPoints = 700;
                    var typeStr = appointment.DonationType == DonationType.Platelets ? "Platelet" : "Plasma";
                    expectedDescription = $"{typeStr} donation completed";
                }

                // Check if this point transaction already exists for the donor on this date
                bool donationPointsExist = donorTransactions.Any(pt =>
                    pt.ActionType == expectedActionType &&
                    pt.TransactionDate.Date == appointment.ScheduledDate.Date);

                if (!donationPointsExist)
                {
                    var monthKey = earnedDate.ToString("yyyy-MM");
                    var donationPt = new PointTransaction
                    {
                        Id = Guid.NewGuid(),
                        DonorId = donor.Id,
                        ActionType = expectedActionType,
                        Points = expectedPoints,
                        Description = expectedDescription,
                        MonthKey = monthKey,
                        TransactionDate = earnedDate
                    };

                    dbContext.PointTransactions.Add(donationPt);
                    donor.TotalPoints += expectedPoints;
                    if (monthKey == currentMonthKey)
                    {
                        donor.MonthlyPoints += expectedPoints;
                    }

                    donationPointsAwardedCount++;
                    pointsAwardedCount += expectedPoints;
                }
            }

            // 3. Award qualified badges that the donor does not have
            var currentBadgesByBadgeId = donor.DonorBadges.ToDictionary(db => db.BadgeId, db => db);
            foreach (var badgeKey in qualifiedBadgeKeys)
            {
                if (badgesByKey.TryGetValue(badgeKey, out var badge))
                {
                    if (!currentBadgesByBadgeId.ContainsKey(badge.Id))
                    {
                        var earnedDate = earnedBadgeDates.TryGetValue(badgeKey, out var date) ? date : dateTimeProvider.UtcNow;
                        var donorBadge = new DonorBadge
                        {
                            Id = Guid.NewGuid(),
                            DonorId = donor.Id,
                            BadgeId = badge.Id,
                            EarnedDate = earnedDate
                        };

                        dbContext.DonorBadges.Add(donorBadge);
                        awardedBadgeKeys.Add(badgeKey);
                        badgesAwardedCount++;

                        if (badge.BonusPoints > 0)
                        {
                            var monthKey = earnedDate.ToString("yyyy-MM");
                            var badgeBonusPt = new PointTransaction
                            {
                                Id = Guid.NewGuid(),
                                DonorId = donor.Id,
                                ActionType = PointActionType.BadgeBonus,
                                Points = badge.BonusPoints,
                                Description = $"Badge bonus: {badge.BadgeName}",
                                MonthKey = monthKey,
                                TransactionDate = earnedDate
                            };

                            dbContext.PointTransactions.Add(badgeBonusPt);
                            donor.TotalPoints += badge.BonusPoints;
                            if (monthKey == currentMonthKey)
                            {
                                donor.MonthlyPoints += badge.BonusPoints;
                            }

                            pointsAwardedCount += badge.BonusPoints;
                        }
                    }
                }
            }

            // 4. Remove over-awarded badges that the donor no longer qualifies for
            foreach (var currentBadge in donor.DonorBadges.ToList())
            {
                if (badgeIdToBadge.TryGetValue(currentBadge.BadgeId, out var badge))
                {
                    var badgeKey = badge.BadgeKey.ToLowerInvariant();
                    if (!qualifiedBadgeKeys.Contains(badgeKey))
                    {
                        dbContext.DonorBadges.Remove(currentBadge);
                        removedBadgeKeys.Add(badgeKey);
                        badgesRemovedCount++;

                        // Revert the points for the badge bonus
                        if (badge.BonusPoints > 0)
                        {
                            var ptToRemove = donorTransactions.FirstOrDefault(pt =>
                                pt.ActionType == PointActionType.BadgeBonus &&
                                pt.Description != null &&
                                (pt.Description.Contains(badge.BadgeName) || pt.Description.Contains(badge.BadgeKey)));

                            if (ptToRemove != null)
                            {
                                dbContext.PointTransactions.Remove(ptToRemove);
                                donor.TotalPoints -= ptToRemove.Points;
                                if (ptToRemove.MonthKey == currentMonthKey)
                                {
                                    donor.MonthlyPoints -= ptToRemove.Points;
                                }
                                pointsDeductedCount += ptToRemove.Points;
                            }
                            else
                            {
                                donor.TotalPoints -= badge.BonusPoints;
                                pointsDeductedCount += badge.BonusPoints;
                            }
                        }
                    }
                }
            }

            // Clamp donor points to avoid negative numbers
            donor.TotalPoints = Math.Max(0, donor.TotalPoints);
            donor.MonthlyPoints = Math.Max(0, donor.MonthlyPoints);

            bool isDonorUpdated = isDonationCountCorrected || 
                                  badgesAwardedCount > 0 || 
                                  badgesRemovedCount > 0 || 
                                  donationPointsAwardedCount > 0;

            if (isDonorUpdated)
            {
                totalDonorsUpdated++;
                totalBadgesAwarded += badgesAwardedCount;
                totalBadgesRemoved += badgesRemovedCount;
                totalPointsAwarded += pointsAwardedCount;
                totalPointsDeducted += pointsDeductedCount;
                totalDonationPointsAwarded += donationPointsAwardedCount;

                details.Add(new DonorReconciliationDetailDto(
                    donor.Id,
                    donor.FullName,
                    previousDonationCount,
                    correctedDonationCount,
                    badgesAwardedCount,
                    badgesRemovedCount,
                    pointsAwardedCount,
                    pointsDeductedCount,
                    donationPointsAwardedCount,
                    awardedBadgeKeys,
                    removedBadgeKeys));
            }
        }

        if (totalDonorsUpdated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<ReconcileBadgesResultDto>.Success(new ReconcileBadgesResultDto(
            totalDonorsChecked,
            totalDonorsUpdated,
            totalDonationCountsCorrected,
            totalBadgesAwarded,
            totalBadgesRemoved,
            totalPointsAwarded,
            totalPointsDeducted,
            totalDonationPointsAwarded,
            details));
        }
        catch (Exception ex)
        {
            return Result<ReconcileBadgesResultDto>.Failure(ex.ToString());
        }
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
