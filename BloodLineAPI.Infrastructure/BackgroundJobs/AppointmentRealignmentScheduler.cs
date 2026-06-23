using System;
using System.Threading;
using BloodLineAPI.Application.Common.Interfaces;
using Hangfire;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class AppointmentRealignmentScheduler(IBackgroundJobClient backgroundJobClient)
    : IAppointmentRealignmentScheduler
{
    public void EnqueueRealignment(Guid centerOrCampaignId, int newSlotDurationMinutes, int newMaxDonorsPerSlot)
    {
        backgroundJobClient.Enqueue<IAppointmentRealignmentService>(service =>
            service.RealignAppointmentsAsync(centerOrCampaignId, newSlotDurationMinutes, newMaxDonorsPerSlot, CancellationToken.None));
    }
}
