using MediatR;

namespace BloodLineAPI.Domain.Common;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}
