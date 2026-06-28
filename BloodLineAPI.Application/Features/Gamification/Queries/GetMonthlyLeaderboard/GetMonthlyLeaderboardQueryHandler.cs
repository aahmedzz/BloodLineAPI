using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetMonthlyLeaderboard;

public sealed class GetMonthlyLeaderboardQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMonthlyLeaderboardQuery, MonthlyLeaderboardResponseDto>
{
    public async Task<MonthlyLeaderboardResponseDto> Handle(GetMonthlyLeaderboardQuery request, CancellationToken cancellationToken)
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
                return new MonthlyLeaderboardResponseDto([], null);
            }

            query = query.Where(d => d.District == requester.District);
        }

        if (request.OnlyMyArea)
        {
            if (string.IsNullOrWhiteSpace(requester.Area))
            {
                return new MonthlyLeaderboardResponseDto([], null);
            }

            query = query.Where(d => d.Area == requester.Area);
        }

        var donors = await query
            .OrderByDescending(d => d.MonthlyPoints)
            .ThenBy(d => d.FullName)
            .Take(top)
            .Select(d => new { d.Id, d.FullName, d.District, d.Area, d.MonthlyPoints })
            .ToListAsync(cancellationToken);

        var entries = donors
            .Select((d, index) => new MonthlyLeaderboardEntryDto(
                d.Id,
                d.FullName,
                d.District,
                d.Area,
                d.MonthlyPoints,
                index + 1,
                d.Id == request.RequestingDonorId))
            .ToList();

        // Calculate my rank
        MonthlyLeaderboardEntryDto? myEntry = null;
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

            var myRank = await rankQuery.CountAsync(d => d.MonthlyPoints > requester.MonthlyPoints, cancellationToken) + 1;

            myEntry = new MonthlyLeaderboardEntryDto(
                requester.Id,
                requester.FullName,
                requester.District,
                requester.Area,
                requester.MonthlyPoints,
                myRank,
                true);
        }

        return new MonthlyLeaderboardResponseDto(entries, myEntry);
    }
}
