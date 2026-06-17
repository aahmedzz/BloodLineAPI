using System.Text.Json;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class AppointmentReminderJob(
    IApplicationDbContext dbContext,
    INotificationSender notificationSender,
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
                await SendAndPersistAsync(
                    appt.Donor.User.Id,
                    appt.DonorId,
                    "Appointment Reminder",
                    $"Your donation appointment at {appt.DonationCenter.Name} is tomorrow at {appt.StartTime:hh\\:mm}.",
                    NotificationType.AppointmentReminder,
                    JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["targetEntity"] = "DonationAppointment",
                        ["targetId"] = appt.Id.ToString()
                    }),
                    ct);
            }

            if (minutesToStart is >= 45 and <= 75)
            {
                await SendAndPersistAsync(
                    appt.Donor.User.Id,
                    appt.DonorId,
                    "Appointment Reminder",
                    $"Your donation appointment at {appt.DonationCenter.Name} starts at {appt.StartTime:hh\\:mm}.",
                    NotificationType.AppointmentReminder,
                    JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["targetEntity"] = "DonationAppointment",
                        ["targetId"] = appt.Id.ToString()
                    }),
                    ct);
            }
        }

        logger.LogInformation("Appointment reminder scan completed at {Now}.", now);
    }

    private async Task SendAndPersistAsync(Guid userId, Guid donorId, string title, string message, NotificationType type, string? actionPayload, CancellationToken ct)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ActionPayload = actionPayload,
            SentDate = dateTimeProvider.UtcNow,
            IsSent = false
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(ct);

        var sent = await notificationSender.SendAsync(donorId, title, message, ct);

        if (sent)
        {
            notification.IsSent = true;
            notification.SentVia = "fcm";
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
