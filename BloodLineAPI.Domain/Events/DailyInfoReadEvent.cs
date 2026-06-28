using BloodLineAPI.Domain.Common;

namespace BloodLineAPI.Domain.Events;

public sealed record DailyInfoReadEvent(
    Guid DonorId,
    DateTime OccurredOn) : IDomainEvent;
