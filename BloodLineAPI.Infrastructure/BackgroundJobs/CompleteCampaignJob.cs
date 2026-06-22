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
    INotificationService notificationService,
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

        int cancelledCount = 0;

        foreach (var appt in appointmentsToCancel)
        {
            var appointmentStart = appt.ScheduledDate.Date.Add(appt.StartTime);
            if (appointmentStart > now)
            {
                // Cancel with gracePeriodMinutes = 0 to allow the system to cancel past/at-time bookings
                appt.Cancel("تم إلغاء الموعد بسبب إنهاء الحملة", now, gracePeriodMinutes: 0);
                cancelledCount++;

                var title = "إلغاء موعد التبرع";
                var message = $"تم إلغاء موعدك في {campaign.Name} بسبب إنهاء حملة التبرع.";
                var payload = new Dictionary<string, string>
                {
                    ["targetEntity"] = "DonationAppointment",
                    ["targetId"] = appt.Id.ToString()
                };

                await notificationService.SendNotificationAsync(
                    appt.DonorId,
                    title,
                    message,
                    NotificationType.AppointmentCancelled,
                    payload,
                    ct);
            }
        }

        // Save status changes to campaign and cancelled appointments
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Campaign {CampaignId} ({CampaignCode}) completed. {CancelledCount} appointments cancelled and notified.",
            campaignId, campaign.CampaignCode, cancelledCount);
    }
}
