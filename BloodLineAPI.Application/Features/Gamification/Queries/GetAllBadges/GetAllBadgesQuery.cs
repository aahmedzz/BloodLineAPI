using MediatR;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetAllBadges;

public sealed record GetAllBadgesQuery : IRequest<IReadOnlyList<BadgeListItemDto>>;
