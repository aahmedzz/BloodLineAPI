using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetAllBadges;

public sealed class GetAllBadgesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllBadgesQuery, IReadOnlyList<BadgeListItemDto>>
{
    public async Task<IReadOnlyList<BadgeListItemDto>> Handle(GetAllBadgesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Badges
            .AsNoTracking()
            .OrderBy(b => b.BadgeType)
            .ThenBy(b => b.BadgeName)
            .Select(b => new BadgeListItemDto(
                b.Id,
                b.BadgeKey,
                b.BadgeName,
                b.BadgeNameAr,
                b.BadgeDescription,
                b.IconUrl,
                b.BadgeType,
                b.BonusPoints))
            .ToListAsync(cancellationToken);
    }
}
