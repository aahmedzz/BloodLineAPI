using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class DeferralExpiryJob(
    IApplicationDbContext dbContext,
    INotificationSender notificationSender,
    ILogger<DeferralExpiryJob> logger,
    IDateTimeProvider dateTimeProvider)
{
    public async Task ExecuteAsync(Guid donorId, CancellationToken ct = default)
    {
        var todayLocal = dateTimeProvider.LocalNow.Date;

        var donor = await dbContext.Donors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == donorId, ct);

        if (donor == null)
        {
            logger.LogWarning("Scheduled status reset job failed: Donor {DonorId} not found.", donorId);
            return;
        }

        // Reset if currently Deferred
        if (donor.Status != DonorStatus.Deferred)
        {
            logger.LogInformation("Donor {DonorId} status is {Status}; skipping status reset.", donorId, donor.Status);
            return;
        }

        // If the lockout date has passed (or no lockout is active anymore)
        if (!donor.LockoutUntil.HasValue || donor.LockoutUntil.Value <= todayLocal)
        {
            var oldStatus = donor.Status;
            donor.Status = DonorStatus.Eligible;
            donor.LockoutUntil = null;

            var notification = new Notification
            {
                UserId = donor.Id,
                Title = "🚨 انتهاء فترة عدم المؤهلية للتبرع",
                Message = "لقد انتهت فترة القيد الطبي الخاصة بك. يمكنك الآن التبرع بالدم بأمان ومساعدة الآخرين!",
                Type = NotificationType.General,
                ActionPayload = null,
                SentDate = dateTimeProvider.UtcNow,
                IsSent = false
            };

            dbContext.Notifications.Add(notification);
            await dbContext.SaveChangesAsync(ct);

            var sent = await notificationSender.SendAsync(donor.Id, notification.Title, notification.Message, ct);
            if (sent)
            {
                notification.IsSent = true;
                notification.SentVia = "fcm";
                await dbContext.SaveChangesAsync(ct);
            }

            logger.LogInformation("Donor {DonorId} status updated from {OldStatus} to Eligible.", donorId, oldStatus);
        }
    }
}
