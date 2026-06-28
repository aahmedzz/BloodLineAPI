using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Common.Services;

public class EmergencyNotificationService(
    IApplicationDbContext dbContext,
    INotificationService notificationService,
    IDonorEligibilityService eligibilityService,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService,
    IEmergencyAppealScheduler appealScheduler)
    : IEmergencyNotificationService
{
    public async Task<Result<SendBulkNotificationResultDto>> SendBulkEmergencyNotificationAsync(
        List<Guid> donorIds,
        string message,
        CancellationToken cancellationToken = default)
    {
        var requestedCount = donorIds.Count;
        var failedDonorIds = new List<Guid>();
        var eligibleDonorsToSend = new List<Donor>();

        // 1. Fetch requested donors
        var donors = await dbContext.Donors
            .Include(d => d.BloodType)
            .Include(d => d.User)
            .Where(d => donorIds.Contains(d.Id))
            .ToListAsync(cancellationToken);

        // Find missing donor IDs
        var foundDonorIds = donors.Select(d => d.Id).ToHashSet();
        foreach (var requestedId in donorIds)
        {
            if (!foundDonorIds.Contains(requestedId))
            {
                failedDonorIds.Add(requestedId);
            }
        }

        // 2. Batch query rate limits (max 1 notification per donor per 24 hours)
        var twentyFourHoursAgo = dateTimeProvider.UtcNow.AddDays(-1);
        var recentNotificationsMap = await dbContext.Notifications
            .Where(n => donorIds.Contains(n.UserId) && n.SentDate >= twentyFourHoursAgo && n.Type == NotificationType.UrgentBloodAppeal)
            .GroupBy(n => n.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        // 3. Sequentially check eligibility (keeps DbContext usage thread-safe)
        foreach (var donor in donors)
        {
            var checkResult = await eligibilityService.CheckEligibilityAsync(donor.Id, DonationType.WholeBlood, cancellationToken);
            if (!checkResult.IsSuccess || checkResult.Data == null)
            {
                failedDonorIds.Add(donor.Id);
                continue;
            }

            var eligibility = checkResult.Data;
            var status = MapEligibilityStatus(donor, eligibility);

            // Server-side enforcement: Only allow sending to "eligible" or "soon"
            if (status != "eligible" && status != "soon")
            {
                failedDonorIds.Add(donor.Id);
                continue;
            }

            // Enforce rate limit (max 1 per 24 hours)
            var recentCount = recentNotificationsMap.GetValueOrDefault(donor.Id, 0);
            if (recentCount >= 1)
            {
                failedDonorIds.Add(donor.Id);
                continue;
            }

            eligibleDonorsToSend.Add(donor);
        }

        if (eligibleDonorsToSend.Count == 0)
        {
            return Result<SendBulkNotificationResultDto>.Success(new SendBulkNotificationResultDto(
                Requested: requestedCount,
                Sent: 0,
                Failed: failedDonorIds.Count,
                FailedDonorIds: failedDonorIds
            ));
        }

        // Create an UrgentBloodAppeal record in the database
        var currentUserIdStr = currentUserService.UserId;
        var currentStaffId = Guid.Empty;
        if (!string.IsNullOrEmpty(currentUserIdStr))
        {
            currentStaffId = Guid.Parse(currentUserIdStr);
        }

        var targetedBloodTypeIds = eligibleDonorsToSend
            .Where(d => d.BloodTypeId.HasValue)
            .Select(d => d.BloodTypeId!.Value)
            .Distinct()
            .ToList();

        var targetedDistricts = eligibleDonorsToSend
            .Where(d => !string.IsNullOrEmpty(d.District))
            .Select(d => d.District!)
            .Distinct()
            .ToList();

        var targetDistrict = targetedDistricts.FirstOrDefault() ?? "Beni Suef";

        var appeal = new UrgentBloodAppeal
        {
            Id = Guid.NewGuid(),
            CreatedByStaffId = currentStaffId,
            Title = "🚨 طلب تبرع دم عاجل",
            Description = message,
            TargetDistrict = targetDistrict,
            TargetBagsNeeded = eligibleDonorsToSend.Count,
            CurrentBagsCollected = 0,
            IsActive = true,
            BroadcastDate = dateTimeProvider.UtcNow
        };

        var targetedBloodTypes = await dbContext.BloodTypes
            .Where(bt => targetedBloodTypeIds.Contains(bt.Id))
            .ToListAsync(cancellationToken);

        foreach (var bt in targetedBloodTypes)
        {
            appeal.TargetedBloodTypes.Add(bt);
        }

        dbContext.UrgentBloodAppeals.Add(appeal);
        await dbContext.SaveChangesAsync(cancellationToken);

        appealScheduler.ScheduleDeactivation(appeal.Id, TimeSpan.FromDays(2));

        // 4. Send notifications via NotificationService (handles DB audit + FCM dispatch)
        var title = "🚨 طلب تبرع دم عاجل";
        var sentCount = 0;
        var payload = new Dictionary<string, string>
        {
            { "targetEntity", "UrgentBloodAppeal" },
            { "targetId", appeal.Id.ToString() }
        };

        foreach (var donor in eligibleDonorsToSend)
        {
            try
            {
                await notificationService.SendNotificationAsync(
                    donor.Id,
                    title,
                    message,
                    NotificationType.UrgentBloodAppeal,
                    payload,
                    cancellationToken: cancellationToken);
                sentCount++;
            }
            catch
            {
                failedDonorIds.Add(donor.Id);
            }
        }

        var resultDto = new SendBulkNotificationResultDto(
            Requested: requestedCount,
            Sent: sentCount,
            Failed: failedDonorIds.Count,
            FailedDonorIds: failedDonorIds
        );

        return Result<SendBulkNotificationResultDto>.Success(resultDto);
    }

    private static string MapEligibilityStatus(Donor donor, DonorEligibilityResult? eligibility)
    {
        if (donor.Status == DonorStatus.Ineligible)
            return "ineligible";

        if (eligibility != null)
        {
            if (eligibility.DeferredUntil.HasValue)
                return "deferred";

            if (eligibility.IsEligible)
                return "eligible";

            if (eligibility.CooldownRemainingDays.HasValue)
                return eligibility.CooldownRemainingDays.Value <= 14 ? "soon" : "not_yet";
        }

        return "eligible";
    }
}
