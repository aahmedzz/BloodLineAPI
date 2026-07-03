using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.BloodEntities;
using BloodLineAPI.Domain.Enums;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using BloodLineAPI.Application.Features.DonorEligibility.Commands.SendEmergencyNotifications;
using BloodLineAPI.Application.Features.DonorEligibility.Queries.GetEmergencyNotificationPreview;
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
    private async Task<(
        List<Donor> EligibleDonors, 
        List<BloodType> TargetedBloodTypes, 
        string Title, 
        string Message, 
        int RequestedCount, 
        List<FailedDonorDto> FailedDonors)> ResolveNotificationDetailsAsync(
            string? selectionMode,
            List<Guid>? donorIds,
            DonorEligibilityFiltersDto? filters,
            List<Guid>? excludedDonorIds,
            CancellationToken cancellationToken)
    {
        List<Guid> targetDonorIds;
        var isFilteredMode = selectionMode != null && 
                             selectionMode.Equals("filtered", StringComparison.OrdinalIgnoreCase);

        if (isFilteredMode)
        {
            if (filters == null)
            {
                throw new ArgumentException("Filters are required in filtered mode.");
            }

            // Apply eligibility filters using our shared pipeline
            var query = dbContext.Donors
                .Include(d => d.BloodType)
                .Include(d => d.User)
                .AsQueryable();

            query = await eligibilityService.FilterDonorsAsync(query, filters, cancellationToken);

            // Exclude the specified donor IDs
            var excludedIds = excludedDonorIds ?? new List<Guid>();
            if (excludedIds.Any())
            {
                query = query.Where(d => !excludedIds.Contains(d.Id));
            }

            // Get the list of matching donor IDs
            targetDonorIds = await query.Select(d => d.Id).ToListAsync(cancellationToken);
        }
        else
        {
            targetDonorIds = donorIds ?? new List<Guid>();
        }

        var requestedCount = targetDonorIds.Count;
        var failedDonors = new List<FailedDonorDto>();
        var eligibleDonorsToSend = new List<Donor>();

        if (requestedCount == 0)
        {
            return (eligibleDonorsToSend, new List<BloodType>(), "🚨 فرصة للمساعدة في إنقاذ حياة", "🚨 فرصة لإنقاذ حياة: هناك حاجة لمتبرعين بالدم حالياً لدعم المرضى المحتاجين.", 0, failedDonors);
        }

        // 1. Fetch requested donors
        var donors = await dbContext.Donors
            .Include(d => d.BloodType)
            .Include(d => d.User)
            .Where(d => targetDonorIds.Contains(d.Id))
            .ToListAsync(cancellationToken);

        // Note: Missing donor IDs (not found in database) are silently skipped as they cannot be contacted anyway.

        // 2. Batch query rate limits (max 1 notification per donor per 24 hours)
        var twentyFourHoursAgo = dateTimeProvider.UtcNow.AddDays(-1);
        var recentNotificationsMap = await dbContext.Notifications
            .Where(n => targetDonorIds.Contains(n.UserId) && n.SentDate >= twentyFourHoursAgo && n.Type == NotificationType.UrgentBloodAppeal)
            .GroupBy(n => n.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        // 3. Sequentially check eligibility (keeps DbContext usage thread-safe)
        foreach (var donor in donors)
        {
            var checkResult = await eligibilityService.CheckEligibilityAsync(donor.Id, DonationType.WholeBlood, cancellationToken);
            if (!checkResult.IsSuccess || checkResult.Data == null)
            {
                // Ineligible -> skip manual call outreach list
                continue;
            }

            var eligibility = checkResult.Data;
            var status = MapEligibilityStatus(donor, eligibility);

            // Server-side enforcement: Only allow sending to "eligible" or "soon"
            if (status != "eligible" && status != "soon")
            {
                // Deferred/Ineligible -> skip manual call outreach list
                continue;
            }

            // Enforce rate limit (max 1 per 24 hours)
            var recentCount = recentNotificationsMap.GetValueOrDefault(donor.Id, 0);
            if (recentCount >= 1)
            {
                // Already notified -> skip
                continue;
            }

            // Remove Donors without App Accounts
            if (donor.User == null || donor.User.PasswordHash == null)
            {
                // Eligible but has no mobile app account. Add to failed list for manual call.
                failedDonors.Add(new FailedDonorDto(
                    donor.Id,
                    donor.FullName,
                    donor.PhoneNumber,
                    donor.BloodType?.FullDisplayname ?? "Unknown",
                    "لا يوجد حساب نشط على تطبيق الهاتف"));
                continue;
            }

            eligibleDonorsToSend.Add(donor);
        }

        if (eligibleDonorsToSend.Count == 0)
        {
            return (eligibleDonorsToSend, new List<BloodType>(), "🚨 فرصة للمساعدة في إنقاذ حياة", "🚨 فرصة لإنقاذ حياة: هناك حاجة لمتبرعين بالدم حالياً لدعم المرضى المحتاجين.", requestedCount, failedDonors);
        }

        var targetedBloodTypeIds = eligibleDonorsToSend
            .Where(d => d.BloodTypeId.HasValue)
            .Select(d => d.BloodTypeId!.Value)
            .Distinct()
            .ToList();

        var targetedBloodTypes = await dbContext.BloodTypes
            .Where(bt => targetedBloodTypeIds.Contains(bt.Id))
            .ToListAsync(cancellationToken);

        var targetedBloodTypesStr = targetedBloodTypes.Any()
            ? string.Join(", ", targetedBloodTypes.Select(bt => bt.FullDisplayname))
            : string.Empty;

        // Title and message - constructed on the backend with encouraging, panic-free text
        var title = "🚨 فرصة للمساعدة في إنقاذ حياة";
        var message = targetedBloodTypes.Any()
            ? $"🚨 فرصة لإنقاذ حياة: فصيلتك الدموية ({targetedBloodTypesStr}) مطلوبة حالياً لدعم حالات بحاجة للتبرع بالدم. تبرعك قد يكون سبباً في إدخال الفرحة والشفاء على قلب مريض وعائلته. نسعد بزيارتك لأقرب مركز تبرع."
            : "🚨 فرصة لإنقاذ حياة: هناك حاجة لمتبرعين بالدم حالياً لدعم المرضى المحتاجين. تبرعك قد يكون سبباً في إدخال الفرحة والشفاء على قلب مريض وعائلته. نسعد بزيارتك لأقرب مركز تبرع.";

        return (eligibleDonorsToSend, targetedBloodTypes, title, message, requestedCount, failedDonors);
    }

    public async Task<Result<SendBulkNotificationResultDto>> SendBulkEmergencyNotificationAsync(
        SendEmergencyNotificationsCommand command,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveNotificationDetailsAsync(
            command.SelectionMode,
            command.DonorIds,
            command.Filters,
            command.ExcludedDonorIds,
            cancellationToken);

        if (resolved.EligibleDonors.Count == 0)
        {
            return Result<SendBulkNotificationResultDto>.Success(new SendBulkNotificationResultDto(
                AppealId: null,
                Requested: resolved.RequestedCount,
                Sent: 0,
                Failed: resolved.FailedDonors.Count,
                FailedDonors: resolved.FailedDonors
            ));
        }

        // Create an UrgentBloodAppeal record in the database
        var currentUserIdStr = currentUserService.UserId;
        var currentStaffId = Guid.Empty;
        if (!string.IsNullOrEmpty(currentUserIdStr))
        {
            currentStaffId = Guid.Parse(currentUserIdStr);
        }

        var targetedDistricts = resolved.EligibleDonors
            .Where(d => !string.IsNullOrEmpty(d.District))
            .Select(d => d.District!)
            .Distinct()
            .ToList();

        var targetDistrict = targetedDistricts.FirstOrDefault() ?? "Beni Suef";

        var appeal = new UrgentBloodAppeal
        {
            Id = Guid.NewGuid(),
            CreatedByStaffId = currentStaffId,
            Title = resolved.Title,
            Description = resolved.Message,
            TargetDistrict = targetDistrict,
            TargetBagsNeeded = resolved.EligibleDonors.Count,
            CurrentBagsCollected = 0,
            IsActive = true,
            BroadcastDate = dateTimeProvider.UtcNow
        };

        foreach (var bt in resolved.TargetedBloodTypes)
        {
            appeal.TargetedBloodTypes.Add(bt);
        }

        dbContext.UrgentBloodAppeals.Add(appeal);
        await dbContext.SaveChangesAsync(cancellationToken);

        appealScheduler.ScheduleDeactivation(appeal.Id, TimeSpan.FromDays(2));

        // 4. Send notifications via NotificationService (handles DB audit + FCM dispatch)
        var sentCount = 0;
        var failedDonors = new List<FailedDonorDto>(resolved.FailedDonors);
        var payload = new Dictionary<string, string>
        {
            { "targetEntity", "UrgentBloodAppeal" },
            { "targetId", appeal.Id.ToString() }
        };

        foreach (var donor in resolved.EligibleDonors)
        {
            try
            {
                var success = await notificationService.SendNotificationAsync(
                    donor.Id,
                    resolved.Title,
                    resolved.Message,
                    NotificationType.UrgentBloodAppeal,
                    payload,
                    cancellationToken: cancellationToken);
                
                if (success)
                {
                    sentCount++;
                }
                else
                {
                    failedDonors.Add(new FailedDonorDto(
                        donor.Id,
                        donor.FullName,
                        donor.PhoneNumber,
                        donor.BloodType?.FullDisplayname ?? "Unknown",
                        "فشل نظام الإشعارات في إرسال إشعار الهاتف"));
                }
            }
            catch (Exception)
            {
                failedDonors.Add(new FailedDonorDto(
                    donor.Id,
                    donor.FullName,
                    donor.PhoneNumber,
                    donor.BloodType?.FullDisplayname ?? "Unknown",
                    "خطأ غير متوقع أثناء إرسال الإشعار"));
            }
        }

        var resultDto = new SendBulkNotificationResultDto(
            AppealId: appeal.Id,
            Requested: resolved.RequestedCount,
            Sent: sentCount,
            Failed: failedDonors.Count,
            FailedDonors: failedDonors
        );

        return Result<SendBulkNotificationResultDto>.Success(resultDto);
    }

    public async Task<Result<NotificationPreviewResponseDto>> GetEmergencyNotificationPreviewAsync(
        GetEmergencyNotificationPreviewQuery query,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveNotificationDetailsAsync(
            query.SelectionMode,
            query.DonorIds,
            query.Filters,
            query.ExcludedDonorIds,
            cancellationToken);

        var preview = new NotificationPreviewResponseDto(
            Title: resolved.Title,
            Message: resolved.Message,
            RecipientCount: resolved.EligibleDonors.Count
        );

        return Result<NotificationPreviewResponseDto>.Success(preview);
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
