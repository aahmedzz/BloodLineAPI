using System;
using System.Threading;
using BloodLineAPI.Application.Common.Interfaces;
using Hangfire;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class DonorStatusScheduler(
    IBackgroundJobClient backgroundJobClient,
    IDateTimeProvider dateTimeProvider)
    : IDonorStatusScheduler
{
    public void ScheduleStatusReset(Guid donorId, DateTime lockoutUntil)
    {
        var delay = lockoutUntil - dateTimeProvider.UtcNow;
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        backgroundJobClient.Schedule<DeferralExpiryJob>(
            job => job.ExecuteAsync(donorId, CancellationToken.None),
            delay);
    }
}
