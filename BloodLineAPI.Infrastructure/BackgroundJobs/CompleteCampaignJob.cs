using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class CompleteCampaignJob(
    IApplicationDbContext dbContext,
    INotificationSender notificationSender,
    ILogger<CompleteCampaignJob> logger,
    IDateTimeProvider dateTimeProvider)
{
    public async Task ExecuteAsync(Guid campaignId, CancellationToken ct = default)
    {
        var campaign = await dbContext.DonationCenters
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct);

        if (campaign == null)
        {
            logger.LogWarning("Campaign {CampaignId} not found for completion.", campaignId);
            return;
        }

        if (campaign.CenterType != CenterType.Campaign)
        {
            logger.LogWarning("Center {CenterId} is not a Campaign. Status update skipped.", campaignId);
            return;
        }

        if (campaign.Status == CenterStatus.Completed)
        {
            logger.LogInformation("Campaign {CampaignId} ({CampaignCode}) is already Completed; skipping.", campaignId, campaign.CampaignCode);
            return;
        }

        // 1. Update status to Completed and clear job references
        campaign.Status = CenterStatus.Completed;
        campaign.ScheduledJobIds = null;

        // 2. Fetch all upcoming pending or confirmed appointments for this campaign
        var now = dateTimeProvider.LocalNow;
        var appointmentsToCancel = await dbContext.DonationAppointments
            .Include(a => a.Donor)
                .ThenInclude(d => d.User)
            .Where(a => a.DonationCenterId == campaignId)
            .Where(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            .ToListAsync(ct);

        var notifications = new List<Notification>();
        var notificationTasks = new List<(Guid DonorId, string Title, string Message, Notification Notif)>();

        foreach (var appt in appointmentsToCancel)
        {
            var appointmentStart = appt.ScheduledDate.Date.Add(appt.StartTime);
            if (appointmentStart > now)
            {
                // Cancel with gracePeriodMinutes = 0 to allow the system to cancel past/at-time bookings
                appt.Cancel("تم إلغاء الموعد بسبب إنهاء الحملة", now, gracePeriodMinutes: 0);

                var title = "إلغاء موعد التبرع";
                var message = $"تم إلغاء موعدك في {campaign.Name} بسبب إنهاء حملة التبرع.";
                var payload = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["targetEntity"] = "DonationAppointment",
                    ["targetId"] = appt.Id.ToString()
                });

                var notification = new Notification
                {
                    UserId = appt.Donor.User.Id,
                    Title = title,
                    Message = message,
                    Type = NotificationType.AppointmentCancelled,
                    ActionPayload = payload,
                    SentDate = dateTimeProvider.UtcNow,
                    IsSent = false
                };

                notifications.Add(notification);
                notificationTasks.Add((appt.DonorId, title, message, notification));
            }
        }

        if (notifications.Any())
        {
            dbContext.Notifications.AddRange(notifications);
        }

        // Save status changes and notifications to database
        await dbContext.SaveChangesAsync(ct);

        // 3. Send Push Notifications (after saving notifications to db)
        foreach (var task in notificationTasks)
        {
            try
            {
                var sent = await notificationSender.SendAsync(task.DonorId, task.Title, task.Message, ct);
                if (sent)
                {
                    task.Notif.IsSent = true;
                    task.Notif.SentVia = "fcm";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send push notification to donor {DonorId} for cancelled appointment.", task.DonorId);
            }
        }

        // Save again to record successful FCM sends
        if (notificationTasks.Any(t => t.Notif.IsSent))
        {
            await dbContext.SaveChangesAsync(ct);
        }

        logger.LogInformation("Campaign {CampaignId} ({CampaignCode}) completed. {CancelledCount} appointments cancelled and notified.",
            campaignId, campaign.CampaignCode, notifications.Count);
    }
}
