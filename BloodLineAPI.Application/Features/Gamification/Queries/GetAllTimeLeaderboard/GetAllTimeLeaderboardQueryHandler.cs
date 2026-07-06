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
                return new AllTimeLeaderboardResponseDto([], null, false);
            }

            query = query.Where(d => d.District == requester.District);
        }

        if (request.OnlyMyArea)
        {
            if (string.IsNullOrWhiteSpace(requester.Area))
            {
                return new AllTimeLeaderboardResponseDto([], null, false);
            }

            query = query.Where(d => d.Area == requester.Area);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var donors = await query
            .OrderByDescending(d => d.TotalPoints)
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
                d.TotalPoints
            })
            .ToListAsync(cancellationToken);

        var entries = donors
            .Select((d, index) => new AllTimeLeaderboardEntryDto(
                d.Id,
                string.Join(" ", new[] { d.FirstName, d.SecondName, d.ThirdName, d.FourthName }.Where(n => !string.IsNullOrWhiteSpace(n))),
                d.District,
                d.Area,
                d.TotalPoints,
                skip + index + 1,
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

        var hasNextPage = (pageNumber * pageSize) < totalCount;

        return new AllTimeLeaderboardResponseDto(entries, myEntry, hasNextPage);
    }
}
