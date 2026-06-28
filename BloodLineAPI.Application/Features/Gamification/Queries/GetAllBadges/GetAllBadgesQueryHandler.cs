using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetAllBadges;

public sealed class GetAllBadgesQueryHandler(
    IApplicationDbContext dbContext,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetAllBadgesQuery, IReadOnlyList<BadgeDetailsDto>>
{
    public async Task<IReadOnlyList<BadgeDetailsDto>> Handle(GetAllBadgesQuery request, CancellationToken cancellationToken)
    {
        var httpReq = httpContextAccessor.HttpContext?.Request;
        var baseUrl = httpReq is not null 
            ? $"{httpReq.Scheme}://{httpReq.Host}{httpReq.PathBase}/" 
            : string.Empty;

        var badges = await dbContext.Badges
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        badges = badges
            .OrderBy(b => b.BadgeType == BadgeType.Milestone ? 0 : 1)
            .ThenBy(b => b.BadgeName)
            .ToList();

        return badges
            .Select(b => new BadgeDetailsDto(
                b.Id,
                b.BadgeKey,
                b.BadgeName,
                b.BadgeNameAr,
                b.BadgeDescription,
                b.BadgeDescriptionAr,
                baseUrl + b.IconUrl,
                b.BadgeType,
                b.BonusPoints))
            .ToList();
    }
}
