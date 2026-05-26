using BloodLineAPI.Domain.Common;

namespace BloodLineAPI.Domain.Events;

public sealed record DonationSentToLabEvent(
    Guid DonorId,
    Guid DonationAppointmentId,
    Guid BloodBagId,
    DateTime OccurredOn) : IDomainEvent;
