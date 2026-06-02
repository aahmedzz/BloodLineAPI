using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class AppointmentNoShowJob(
    IApplicationDbContext dbContext,
    ILogger<AppointmentNoShowJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        if (dbContext is DbContext efContext)
        {
            await efContext.Database.ExecuteSqlRawAsync(
                "UPDATE DonationAppointments SET Source = 'MobileApp' WHERE Source = '' OR Source IS NULL", ct);
            await efContext.Database.ExecuteSqlRawAsync(
                "UPDATE DonationAppointments SET DonationStatus = 'Pending' WHERE DonationStatus = '' OR DonationStatus IS NULL", ct);
        }

        var pastPendingAppointments = await dbContext.DonationAppointments
            .Where(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            .Where(a => a.Source == DonationSource.MobileApp)
            .Where(a => a.DonationStatus == DonationStatus.Pending)
            .Where(a => a.CheckInTime == null)
            .Where(a => a.ScheduledDate.Date <= now.Date)
            .ToListAsync(ct);

        var noShowCount = 0;
        foreach (var app in pastPendingAppointments)
        {
            var slotEnd = app.ScheduledDate.Date.Add(app.EndTime);
            if (slotEnd < now)
            {
                try
                {
                    app.MarkNoShow();
                    noShowCount++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to mark appointment {AppointmentId} as no-show.", app.Id);
                }
            }
        }

        if (noShowCount > 0)
        {
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Marked {Count} past-due appointments as no-show at {Now}.", noShowCount, now);
        }
    }
}
