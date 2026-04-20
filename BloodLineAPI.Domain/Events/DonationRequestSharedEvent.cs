using BloodLineAPI.Domain.Common;

namespace BloodLineAPI.Domain.Events;

public sealed record DonationRequestSharedEvent(
    Guid DonorId,
    Guid UrgentBloodAppealId,
    DateTime OccurredOn) : IDomainEvent;
