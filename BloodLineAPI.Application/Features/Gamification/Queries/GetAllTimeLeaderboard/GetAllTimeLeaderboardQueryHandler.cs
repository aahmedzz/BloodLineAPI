using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetAllTimeLeaderboard;

public sealed class GetAllTimeLeaderboardQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllTimeLeaderboardQuery, AllTimeLeaderboardResponseDto>
{
    public async Task<AllTimeLeaderboardResponseDto> Handle(GetAllTimeLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var top = request.Top <= 0 ? 10 : Math.Min(request.Top, 100);

        var requester = await dbContext.Donors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.RequestingDonorId, cancellationToken);

        if (requester is null)
        {
            throw new NotFoundException("Donor", request.RequestingDonorId);
        }

        var query = dbContext.Donors
            .AsNoTracking()
            .Where(d => d.AllowLeaderboardVisibility);

        if (request.OnlyMyDistrict)
        {
            if (string.IsNullOrWhiteSpace(requester.District))
            {
                return new AllTimeLeaderboardResponseDto([], null);
            }

            query = query.Where(d => d.District == requester.District);
        }

        if (request.OnlyMyArea)
        {
            if (string.IsNullOrWhiteSpace(requester.Area))
            {
                return new AllTimeLeaderboardResponseDto([], null);
            }

            query = query.Where(d => d.Area == requester.Area);
        }

        var donors = await query
            .OrderByDescending(d => d.TotalPoints)
            .ThenBy(d => d.FullName)
            .Take(top)
            .Select(d => new { d.Id, d.FullName, d.District, d.Area, d.TotalPoints })
            .ToListAsync(cancellationToken);

        var entries = donors
            .Select((d, index) => new AllTimeLeaderboardEntryDto(
                d.Id,
                d.FullName,
                d.District,
                d.Area,
                d.TotalPoints,
                index + 1,
                d.Id == request.RequestingDonorId))
            .ToList();

        // Calculate my rank
        AllTimeLeaderboardEntryDto? myEntry = null;
        if (requester.AllowLeaderboardVisibility)
        {
            var rankQuery = dbContext.Donors
                .AsNoTracking()
                .Where(d => d.AllowLeaderboardVisibility);

            if (request.OnlyMyDistrict && !string.IsNullOrWhiteSpace(requester.District))
            {
                rankQuery = rankQuery.Where(d => d.District == requester.District);
            }
            if (request.OnlyMyArea && !string.IsNullOrWhiteSpace(requester.Area))
            {
                rankQuery = rankQuery.Where(d => d.Area == requester.Area);
            }

            var myRank = await rankQuery.CountAsync(d => d.TotalPoints > requester.TotalPoints, cancellationToken) + 1;

            myEntry = new AllTimeLeaderboardEntryDto(
                requester.Id,
                requester.FullName,
                requester.District,
                requester.Area,
                requester.TotalPoints,
                myRank,
                true);
        }

        return new AllTimeLeaderboardResponseDto(entries, myEntry);
    }
}
