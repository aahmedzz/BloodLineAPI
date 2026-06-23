using System;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IAppointmentRealignmentScheduler
{
    void EnqueueRealignment(Guid centerOrCampaignId, int newSlotDurationMinutes, int newMaxDonorsPerSlot);
}
