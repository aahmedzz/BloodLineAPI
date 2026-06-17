using System;
using System.Collections.Generic;
using System.Threading;
using BloodLineAPI.Application.Common.Interfaces;
using Hangfire;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class CampaignScheduler(
    IBackgroundJobClient backgroundJobClient,
    IDateTimeProvider dateTimeProvider)
    : ICampaignScheduler
{
    public string? ScheduleActivation(Guid campaignId, DateTime localStartDateTime)
    {
        var utcStart = dateTimeProvider.ToUtcTime(localStartDateTime);
        var delay = utcStart - dateTimeProvider.UtcNow;
        if (delay <= TimeSpan.Zero)
        {
            return null;
        }

        return backgroundJobClient.Schedule<ActivateCampaignJob>(
            job => job.ExecuteAsync(campaignId, CancellationToken.None),
            delay);
    }

    public string? ScheduleDeactivation(Guid campaignId, DateTime localEndDateTime)
    {
        var utcEnd = dateTimeProvider.ToUtcTime(localEndDateTime);
        var delay = utcEnd - dateTimeProvider.UtcNow;
        if (delay <= TimeSpan.Zero)
        {
            return null;
        }

        return backgroundJobClient.Schedule<DeactivateCampaignJob>(
            job => job.ExecuteAsync(campaignId, CancellationToken.None),
            delay);
    }

    public string? ScheduleCompletion(Guid campaignId, DateTime localEndDateTime)
    {
        var utcEnd = dateTimeProvider.ToUtcTime(localEndDateTime);
        var delay = utcEnd - dateTimeProvider.UtcNow;
        if (delay <= TimeSpan.Zero)
        {
            return null;
        }

        return backgroundJobClient.Schedule<CompleteCampaignJob>(
            job => job.ExecuteAsync(campaignId, CancellationToken.None),
            delay);
    }

    public void UnscheduleJobs(IEnumerable<string> jobIds)
    {
        if (jobIds != null)
        {
            foreach (var jobId in jobIds)
            {
                if (!string.IsNullOrWhiteSpace(jobId))
                {
                    backgroundJobClient.Delete(jobId);
                }
            }
        }
    }
}
