using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class AppointmentReminderJob(
    IApplicationDbContext dbContext,
    INotificationSender notificationSender,
    ILogger<AppointmentReminderJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var maxHorizon = now.AddHours(25);

        var appointments = await dbContext.DonationAppointments
            .AsNoTracking()
            .Where(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            .Where(a => a.ScheduledDate >= now.Date && a.ScheduledDate <= maxHorizon.Date)
            .Include(a => a.DonationCenter)
            .ToListAsync(ct);

        foreach (var appt in appointments)
        {
            var appointmentStart = appt.ScheduledDate.Date.Add(appt.StartTime);
            var minutesToStart = (appointmentStart - now).TotalMinutes;

            if (minutesToStart is >= 1425 and <= 1455)
            {
                await notificationSender.SendAsync(
                    appt.DonorId,
                    "Appointment Reminder",
                    $"Your donation appointment at {appt.DonationCenter.Name} is tomorrow at {appt.StartTime:hh\\:mm}.",
                    ct);
            }

            if (minutesToStart is >= 45 and <= 75)
            {
                await notificationSender.SendAsync(
                    appt.DonorId,
                    "Appointment Reminder",
                    $"Your donation appointment at {appt.DonationCenter.Name} starts at {appt.StartTime:hh\\:mm}.",
                    ct);
            }
        }

        logger.LogInformation("Appointment reminder scan completed at {Now}.", now);
    }
}
