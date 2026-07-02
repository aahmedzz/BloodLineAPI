using System;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class CooldownExpiryJob(
    IApplicationDbContext dbContext,
    IDonorEligibilityService eligibilityService,
    INotificationService notificationService,
    ILogger<CooldownExpiryJob> logger)
{
    public async Task ExecuteAsync(Guid donorId, CancellationToken ct = default)
    {
        var donor = await dbContext.Donors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == donorId, ct);

        if (donor == null)
        {
            logger.LogWarning("Scheduled cooldown reminder failed: Donor {DonorId} not found.", donorId);
            return;
        }

        // Real-time eligibility check
        var eligibilityResult = await eligibilityService.CheckEligibilityAsync(donorId, DonationType.WholeBlood, ct);
        if (!eligibilityResult.IsSuccess || eligibilityResult.Data == null || !eligibilityResult.Data.IsEligible)
        {
            logger.LogInformation("Donor {DonorId} is not currently eligible to donate (Reason: {Reason}); skipping cooldown expiry notification.", 
                donorId, 
                eligibilityResult.Data?.RejectionReason ?? eligibilityResult.Error ?? "Unknown eligibility check failure");
            return;
        }

        // Send DonationReminder notification
        await notificationService.SendNotificationAsync(
            donor.Id,
            "🎉 يمكنك التبرع بالدم الآن!",
            "عزيزي المتبرع، لقد انتهت فترة الانتظار الخاصة بتبرعك السابق. يمكنك الآن حجز موعد جديد للمساهمة في إنقاذ الأرواح. شكراً لعطائك المستمر!",
            NotificationType.DonationReminder,
            payload: null,
            ct);

        logger.LogInformation("Sent cooldown expiry reminder to donor {DonorId}.", donorId);
    }
}
