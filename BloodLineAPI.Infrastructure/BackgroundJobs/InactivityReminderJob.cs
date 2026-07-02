using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class InactivityReminderJob(
    IApplicationDbContext dbContext,
    INotificationService notificationService,
    IDateTimeProvider dateTimeProvider,
    ILogger<InactivityReminderJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var targetDate = dateTimeProvider.LocalNow.Date.AddMonths(-6);

        // Fetch eligible donors with app accounts whose inactivity hit exactly the 6-month mark today
        var inactiveDonors = await dbContext.Donors
            .Include(d => d.User)
            .Where(d => d.Status == DonorStatus.Eligible)
            .Where(d => d.User != null && d.User.PasswordHash != null)
            .Where(d => (d.LastDonationDate.HasValue && d.LastDonationDate.Value.Date == targetDate) ||
                        (!d.LastDonationDate.HasValue && d.CreatedAt.Date == targetDate))
            .ToListAsync(ct);

        logger.LogInformation("Found {Count} inactive donors matching target inactivity date {TargetDate}.", inactiveDonors.Count, targetDate);

        var notifiedCount = 0;
        foreach (var donor in inactiveDonors)
        {
            try
            {
                await notificationService.SendNotificationAsync(
                    donor.Id,
                    "نفتقدك! 🩸",
                    "لقد مر 6 أشهر منذ آخر تبرع لك. تبرعك بالدم يمكن أن ينقذ حياة 3 أشخاص! احجز موعدك اليوم لتصنع فرقاً.",
                    NotificationType.DonationReminder,
                    payload: null,
                    ct);

                notifiedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send inactivity reminder to donor {DonorId}.", donor.Id);
            }
        }

        if (notifiedCount > 0)
        {
            logger.LogInformation("Inactivity reminder scan complete. Dispatched {Count} notifications.", notifiedCount);
        }
    }
}
