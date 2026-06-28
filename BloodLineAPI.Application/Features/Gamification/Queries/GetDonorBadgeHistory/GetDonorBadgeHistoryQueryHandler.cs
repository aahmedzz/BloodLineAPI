using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetDonorBadgeHistory;

public sealed class GetDonorBadgeHistoryQueryHandler(
    IApplicationDbContext dbContext,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetDonorBadgeHistoryQuery, IReadOnlyList<BadgeHistoryItemDto>>
{
    private static readonly Dictionary<string, int> MilestoneTargets = new()
    {
        { "giver", 1 },
        { "helper", 3 },
        { "hero", 5 },
        { "life_saver", 10 },
        { "monqez", 11 }
    };

    public async Task<IReadOnlyList<BadgeHistoryItemDto>> Handle(GetDonorBadgeHistoryQuery request, CancellationToken cancellationToken)
    {
        var httpReq = httpContextAccessor.HttpContext?.Request;
        var baseUrl = httpReq is not null 
            ? $"{httpReq.Scheme}://{httpReq.Host}{httpReq.PathBase}/" 
            : string.Empty;

        var donor = await dbContext.Donors
            .AsNoTracking()
            .Include(d => d.DonorBadges)
            .FirstOrDefaultAsync(d => d.Id == request.DonorId, cancellationToken);

        if (donor is null)
        {
            throw new NotFoundException("Donor", request.DonorId);
        }

        var allBadges = await dbContext.Badges
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var earnedBadgesByBadgeId = donor.DonorBadges
            .ToDictionary(db => db.BadgeId, db => db.EarnedDate);

        // Find the active milestone badge that is currently in progress
        var unearnedMilestoneBadges = allBadges
            .Where(b => b.BadgeType == BadgeType.Milestone && !earnedBadgesByBadgeId.ContainsKey(b.Id))
            .OrderBy(b => MilestoneTargets.GetValueOrDefault(b.BadgeKey.ToLowerInvariant(), 0))
            .ToList();

        var activeMilestoneBadgeId = unearnedMilestoneBadges.FirstOrDefault()?.Id;

        return allBadges
            .OrderBy(b => b.BadgeType == BadgeType.Milestone ? 0 : 1)
            .ThenBy(b => b.BadgeType == BadgeType.Milestone 
                ? MilestoneTargets.GetValueOrDefault(b.BadgeKey.ToLowerInvariant(), 999) 
                : 999)
            .ThenBy(b => b.BadgeName)
            .Select(b =>
            {
                var isEarned = earnedBadgesByBadgeId.TryGetValue(b.Id, out var earnedDate);
                var isMilestone = b.BadgeType == BadgeType.Milestone;

                var isProgressive = isMilestone;
                var targetProgress = isMilestone ? MilestoneTargets.GetValueOrDefault(b.BadgeKey.ToLowerInvariant(), 1) : 1;

                string status;
                int currentProgress;
                DateTime? unlockedDate = null;

                if (isEarned)
                {
                    status = "Unlocked";
                    currentProgress = targetProgress;
                    unlockedDate = earnedDate;
                }
                else
                {
                    if (isMilestone)
                    {
                        if (b.Id == activeMilestoneBadgeId)
                        {
                            status = "InProgress";
                            currentProgress = donor.TotalDonationCount;
                        }
                        else
                        {
                            status = "Locked";
                            currentProgress = 0;
                        }
                    }
                    else
                    {
                        status = "Locked";
                        currentProgress = 0;
                    }
                }

                return new BadgeHistoryItemDto(
                    b.Id,
                    b.BadgeKey,
                    b.BadgeName,
                    b.BadgeNameAr,
                    b.BadgeDescription,
                    b.BadgeDescriptionAr,
                    baseUrl + b.IconUrl,
                    b.BadgeType,
                    b.BonusPoints,
                    isProgressive,
                    status,
                    currentProgress,
                    targetProgress,
                    unlockedDate);
            })
            .ToList();
    }
}
