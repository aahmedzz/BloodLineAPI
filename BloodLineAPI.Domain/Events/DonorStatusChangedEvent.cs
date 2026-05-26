using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Domain.Events;

public sealed record DonorStatusChangedEvent(
    Guid DonorId,
    DonorStatus OldStatus,
    DonorStatus NewStatus,
    string? Reason,
    DateTime OccurredOn) : IDomainEvent;
