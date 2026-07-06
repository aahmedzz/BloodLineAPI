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
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100);
        var skip = (pageNumber - 1) * pageSize;

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
                return new MonthlyLeaderboardResponseDto([], null, false);
            }

            query = query.Where(d => d.District == requester.District);
        }

        if (request.OnlyMyArea)
        {
            if (string.IsNullOrWhiteSpace(requester.Area))
            {
                return new MonthlyLeaderboardResponseDto([], null, false);
            }

            query = query.Where(d => d.Area == requester.Area);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var donors = await query
            .OrderByDescending(d => d.MonthlyPoints)
            .ThenBy(d => d.FirstName)
            .ThenBy(d => d.SecondName)
            .Skip(skip)
            .Take(pageSize)
            .Select(d => new
            {
                d.Id,
                d.FirstName,
                d.SecondName,
                d.ThirdName,
                d.FourthName,
                d.District,
                d.Area,
                d.MonthlyPoints
            })
            .ToListAsync(cancellationToken);

        var entries = donors
            .Select((d, index) => new MonthlyLeaderboardEntryDto(
                d.Id,
                string.Join(" ", new[] { d.FirstName, d.SecondName, d.ThirdName, d.FourthName }.Where(n => !string.IsNullOrWhiteSpace(n))),
                d.District,
                d.Area,
                d.MonthlyPoints,
                skip + index + 1,
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

        var hasNextPage = (pageNumber * pageSize) < totalCount;

        return new MonthlyLeaderboardResponseDto(entries, myEntry, hasNextPage);
    }
}
