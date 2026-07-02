using System.Text.Json;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class AppointmentReminderJob(
    IApplicationDbContext dbContext,
    INotificationService notificationService,
    ILogger<AppointmentReminderJob> logger,
    IDateTimeProvider dateTimeProvider)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var now = dateTimeProvider.LocalNow;
        var maxHorizon = now.AddHours(25);

        var appointments = await dbContext.DonationAppointments
            .AsNoTracking()
            .Where(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            .Where(a => a.ScheduledDate >= now.Date && a.ScheduledDate <= maxHorizon.Date)
            .Include(a => a.DonationCenter)
            .Include(a => a.Donor)
                .ThenInclude(d => d.User)
            .ToListAsync(ct);

        foreach (var appt in appointments)
        {
            var appointmentStart = appt.ScheduledDate.Date.Add(appt.StartTime);
            var minutesToStart = (appointmentStart - now).TotalMinutes;

            if (minutesToStart is >= 1425 and <= 1455)
            {
                await notificationService.SendNotificationAsync(
                    appt.DonorId,
                    "تذكير بموعد التبرع",
                    $"تذكير: موعد تبرعك بالدم في {appt.DonationCenter.Name} غداً الساعة {appt.StartTime:hh\\:mm}.",
                    NotificationType.AppointmentReminder,
                    new Dictionary<string, string>
                    {
                        ["targetEntity"] = "DonationAppointment",
                        ["targetId"] = appt.Id.ToString()
                    },
                    ct);
            }

            if (minutesToStart is >= 45 and <= 75)
            {
                await notificationService.SendNotificationAsync(
                    appt.DonorId,
                    "تذكير بموعد التبرع",
                    $"تذكير: يبدأ موعد تبرعك بالدم في {appt.DonationCenter.Name} خلال ساعة (الساعة {appt.StartTime:hh\\:mm}).",
                    NotificationType.AppointmentReminder,
                    new Dictionary<string, string>
                    {
                        ["targetEntity"] = "DonationAppointment",
                        ["targetId"] = appt.Id.ToString()
                    },
                    ct);
            }
        }

        logger.LogInformation("Appointment reminder scan completed at {Now}.", now);
    }


}
