using BloodLineAPI.Domain.Common;

namespace BloodLineAPI.Domain.Events;

public sealed record ProfileCompletedEvent(
    Guid DonorId,
    DateTime OccurredOn) : IDomainEvent;
