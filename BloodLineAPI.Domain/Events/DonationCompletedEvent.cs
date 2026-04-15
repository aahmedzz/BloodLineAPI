using BloodLineAPI.Domain.Common;

namespace BloodLineAPI.Domain.Events;

public sealed record DonationCompletedEvent(
    Guid DonorId,
    Guid DonationAppointmentId,
    bool IsEmergencyDonation,
    DateTime OccurredOn) : IDomainEvent;
