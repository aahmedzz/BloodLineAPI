using BloodLineAPI.Domain.Common;

namespace BloodLineAPI.Domain.Events;

public sealed record DailyInfoSharedEvent(
    Guid DonorId,
    DateTime OccurredOn) : IDomainEvent;
