using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Domain.Events;

public sealed record DonationRegisteredEvent(
    Guid DonorId,
    Guid DonationAppointmentId,
    DonationSource DonationSource,
    DonationType DonationType,
    bool IsNewDonor,
    bool HasAppAccount,
    DateTime OccurredOn) : IDomainEvent;
