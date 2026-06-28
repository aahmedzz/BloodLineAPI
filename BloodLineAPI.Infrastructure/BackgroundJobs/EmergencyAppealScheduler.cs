using System;
using System.Threading;
using BloodLineAPI.Application.Common.Interfaces;
using Hangfire;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class EmergencyAppealScheduler(IBackgroundJobClient backgroundJobClient)
    : IEmergencyAppealScheduler
{
    public string ScheduleDeactivation(Guid appealId, TimeSpan delay)
    {
        return backgroundJobClient.Schedule<DeactivateUrgentBloodAppealJob>(
            job => job.ExecuteAsync(appealId, CancellationToken.None),
            delay);
    }
}
