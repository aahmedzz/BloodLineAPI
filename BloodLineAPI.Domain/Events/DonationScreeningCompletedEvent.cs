using BloodLineAPI.Domain.Common;

namespace BloodLineAPI.Domain.Events;

public sealed record DonationScreeningCompletedEvent(
    Guid DonorId,
    Guid DonationAppointmentId,
    Guid MedicalScreeningId,
    bool IsEligible,
    DateTime OccurredOn) : IDomainEvent;
