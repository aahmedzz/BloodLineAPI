using MediatR;

namespace BloodLineAPI.Application.Features.Notifications.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery(Guid UserId) : IRequest<int>;
