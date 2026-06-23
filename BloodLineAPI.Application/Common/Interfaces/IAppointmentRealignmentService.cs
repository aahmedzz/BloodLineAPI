using System;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IAppointmentRealignmentService
{
    Task RealignAppointmentsAsync(
        Guid centerOrCampaignId,
        int newSlotDurationMinutes,
        int newMaxDonorsPerSlot,
        CancellationToken cancellationToken = default);
}
