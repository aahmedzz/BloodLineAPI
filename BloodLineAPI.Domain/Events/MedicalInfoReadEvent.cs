using BloodLineAPI.Domain.Common;

namespace BloodLineAPI.Domain.Events;

public sealed record MedicalInfoReadEvent(
    Guid DonorId,
    Guid MedicalInfoId,
    DateTime OccurredOn) : IDomainEvent;
