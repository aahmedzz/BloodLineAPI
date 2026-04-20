using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetDonorGamificationProfile;

public sealed class GetDonorGamificationProfileQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetDonorGamificationProfileQuery, DonorGamificationProfileDto>
{
    public async Task<DonorGamificationProfileDto> Handle(GetDonorGamificationProfileQuery request, CancellationToken cancellationToken)
    {
        var donor = await dbContext.Donors
            .AsNoTracking()
            .Include(d => d.DonorBadges)
                .ThenInclude(db => db.Badge)
            .FirstOrDefaultAsync(d => d.Id == request.DonorId, cancellationToken);

        if (donor is null)
        {
            throw new NotFoundException("Donor", request.DonorId);
        }

        int? monthlyRank = null;
        int? allTimeRank = null;

        if (donor.AllowLeaderboardVisibility)
        {
            monthlyRank = await dbContext.Donors
                .AsNoTracking()
                .Where(d => d.AllowLeaderboardVisibility && d.MonthlyPoints > donor.MonthlyPoints)
                .CountAsync(cancellationToken) + 1;

            allTimeRank = await dbContext.Donors
                .AsNoTracking()
                .Where(d => d.AllowLeaderboardVisibility && d.TotalPoints > donor.TotalPoints)
                .CountAsync(cancellationToken) + 1;
        }

        var badges = donor.DonorBadges
            .OrderByDescending(db => db.EarnedDate)
            .Select(db => new DonorBadgeDto(
                db.Badge.BadgeKey,
                db.Badge.BadgeName,
                db.Badge.BadgeNameAr,
                db.Badge.IconUrl,
                db.EarnedDate,
                db.Badge.BonusPoints))
            .ToList();

        return new DonorGamificationProfileDto(
            donor.Id,
            donor.FullName,
            donor.TotalPoints,
            donor.MonthlyPoints,
            donor.TotalDonationCount,
            monthlyRank,
            allTimeRank,
            badges);
    }
}
