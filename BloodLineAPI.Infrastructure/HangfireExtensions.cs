using System.Threading;
using BloodLineAPI.Infrastructure.BackgroundJobs;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BloodLineAPI.Infrastructure;

public static class HangfireExtensions
{
    public static void UseBloodLineRecurringJobs(this IApplicationBuilder app)
    {
        var recurringJobManager = app.ApplicationServices.GetService<IRecurringJobManager>();
        if (recurringJobManager is not null)
        {
            recurringJobManager.AddOrUpdate<AppointmentReminderJob>(
                "appointment-reminders",
                job => job.ExecuteAsync(CancellationToken.None),
                "*/15 * * * *");

            recurringJobManager.AddOrUpdate<AppointmentNoShowJob>(
                "appointment-no-shows",
                job => job.ExecuteAsync(CancellationToken.None),
                "0 * * * *");

            recurringJobManager.AddOrUpdate<ChatHistoryCleanupJob>(
                "chat-history-cleanup",
                job => job.ExecuteAsync(CancellationToken.None),
                "0 3 * * *"); // Daily at 3:00 AM UTC

            recurringJobManager.AddOrUpdate<BloodBagExpiryJob>(
                "blood-bag-expiry",
                job => job.ExecuteAsync(),
                "0 0 * * *"); // Daily at midnight UTC

            recurringJobManager.AddOrUpdate<ResetMonthlyPointsJob>(
                "reset-monthly-points",
                job => job.ExecuteAsync(CancellationToken.None),
                "0 0 1 * *"); // Monthly at midnight on the 1st

            recurringJobManager.AddOrUpdate<InactivityReminderJob>(
                "inactivity-reminders",
                job => job.ExecuteAsync(CancellationToken.None),
                "0 12 * * *"); // Daily at 12:00 PM UTC/Local
        }
    }
}
