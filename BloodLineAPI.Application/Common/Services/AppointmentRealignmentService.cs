using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Application.Common.Services;

public sealed class AppointmentRealignmentService(
    IApplicationDbContext dbContext,
    INotificationService notificationService,
    IDateTimeProvider dateTimeProvider,
    ILogger<AppointmentRealignmentService> logger)
    : IAppointmentRealignmentService
{
    public async Task RealignAppointmentsAsync(
        Guid centerOrCampaignId,
        int newSlotDurationMinutes,
        int newMaxDonorsPerSlot,
        CancellationToken cancellationToken = default)
    {
        var center = await dbContext.DonationCenters
            .Include(c => c.OpeningHours)
            .Include(c => c.CenterExclusions)
            .FirstOrDefaultAsync(c => c.Id == centerOrCampaignId, cancellationToken);

        if (center == null)
        {
            logger.LogWarning("Donation center or campaign {CenterId} not found for appointment realignment.", centerOrCampaignId);
            return;
        }

        var todayDate = dateTimeProvider.LocalNow.Date;

        // Retrieve future active appointments (Pending or Confirmed) for this center/campaign
        var appointments = await dbContext.DonationAppointments
            .Include(a => a.Donor)
                .ThenInclude(d => d.User)
            .Where(a => a.DonationCenterId == centerOrCampaignId)
            .Where(a => a.ScheduledDate >= todayDate)
            .Where(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            .ToListAsync(cancellationToken);

        if (appointments.Count == 0)
        {
            logger.LogInformation("No future active appointments found for center or campaign {CenterId} to realign.", centerOrCampaignId);
            return;
        }

        // Group appointments by date so we process day-by-day
        var appointmentsByDate = appointments.GroupBy(a => a.ScheduledDate.Date);
        var appointmentsToNotify = new List<DonationAppointment>();

        foreach (var group in appointmentsByDate)
        {
            var date = group.Key;
            
            // Generate the new slots for this date based on updated hours/exclusions
            var newSlots = center.GenerateTimeSlotsForDate(
                date,
                center.CenterExclusions.ToList(),
                center.OpeningHours.ToList());

            if (newSlots.Count == 0)
            {
                // The center/campaign is closed on this date under new settings! 
                // We should keep them, but since center/campaign is closed, we might need to handle it.
                // For safety, if no slots exist, we do not shift them (they will remain at their old times).
                logger.LogWarning("New configuration resulted in zero slots for date {Date} at center/campaign {CenterName}. Appointments cannot be realigned.", date, center.Name);
                continue;
            }

            // Track capacity count for each slot start time
            var slotCapacityTracker = newSlots.ToDictionary(s => s.Start, _ => 0);

            // Sort appointments by their original start time to preserve booking order priority
            var dayAppointments = group.OrderBy(a => a.StartTime).ToList();

            foreach (var appt in dayAppointments)
            {
                // Find the closest available slot that has capacity
                var availableSlots = newSlots
                    .Where(s => slotCapacityTracker[s.Start] < newMaxDonorsPerSlot)
                    .OrderBy(s => Math.Abs((s.Start - appt.StartTime).Ticks))
                    .ToList();

                if (availableSlots.Count > 0)
                {
                    var targetSlot = availableSlots.First();
                    slotCapacityTracker[targetSlot.Start]++;

                    if (appt.StartTime != targetSlot.Start)
                    {
                        var originalStart = appt.StartTime;
                        appt.RealignSlot(targetSlot.Start, targetSlot.End);
                        appointmentsToNotify.Add(appt);
                        
                        logger.LogInformation("Realigned appointment {ApptId} from {OldStart} to {NewStart} on {Date}", 
                            appt.Id, originalStart, targetSlot.Start, date);
                    }
                }
                else
                {
                    // Fallback: If all slots are full, assign to the closest slot regardless of capacity
                    var targetSlot = newSlots
                        .OrderBy(s => Math.Abs((s.Start - appt.StartTime).Ticks))
                        .First();
                    
                    slotCapacityTracker[targetSlot.Start]++;

                    if (appt.StartTime != targetSlot.Start)
                    {
                        var originalStart = appt.StartTime;
                        appt.RealignSlot(targetSlot.Start, targetSlot.End);
                        appointmentsToNotify.Add(appt);

                        logger.LogInformation("Realigned appointment {ApptId} (Capacity Overrun) from {OldStart} to {NewStart} on {Date}", 
                            appt.Id, originalStart, targetSlot.Start, date);
                    }
                }
            }
        }

        if (appointmentsToNotify.Count > 0)
        {
            // Save modified appointment times to database
            await dbContext.SaveChangesAsync(cancellationToken);

            // Send push notifications to affected users
            foreach (var appt in appointmentsToNotify)
            {
                try
                {
                    var typeName = center.CenterType == CenterType.Campaign ? "الحملة" : "المركز";
                    var title = "تعديل موعد التبرع";
                    var message = $"عزيزي المتبرع، تم تحديث مواعيد العمل في {typeName}. تم تعديل موعد تبرعك بتاريخ {appt.ScheduledDate:yyyy-MM-dd} ليصبح في تمام الساعة {appt.StartTime:hh\\:mm}.";

                    var payload = new Dictionary<string, string>
                    {
                        ["targetEntity"] = "DonationAppointment",
                        ["targetId"] = appt.Id.ToString()
                    };

                    await notificationService.SendNotificationAsync(
                        appt.DonorId,
                        title,
                        message,
                        NotificationType.AppointmentRescheduled,
                        payload,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send realignment notification to donor {DonorId} for appointment {ApptId}", 
                        appt.DonorId, appt.Id);
                }
            }
        }
    }
}
