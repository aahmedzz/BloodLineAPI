using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetMonthlyLeaderboard;

public sealed class GetMonthlyLeaderboardQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMonthlyLeaderboardQuery, IReadOnlyList<MonthlyLeaderboardEntryDto>>
{
    public async Task<IReadOnlyList<MonthlyLeaderboardEntryDto>> Handle(GetMonthlyLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var top = request.Top <= 0 ? 10 : Math.Min(request.Top, 100);

        var requesterLocation = await dbContext.Donors
            .AsNoTracking()
            .Where(d => d.Id == request.RequestingDonorId)
            .Select(d => new { d.District, d.Area })
            .FirstOrDefaultAsync(cancellationToken);

        if (requesterLocation is null)
        {
            throw new NotFoundException("Donor", request.RequestingDonorId);
        }

        var query = dbContext.Donors
            .AsNoTracking()
            .Where(d => d.AllowLeaderboardVisibility);

        if (request.OnlyMyDistrict)
        {
            if (string.IsNullOrWhiteSpace(requesterLocation.District))
            {
                return [];
            }

            query = query.Where(d => d.District == requesterLocation.District);
        }

        if (request.OnlyMyArea)
        {
            if (string.IsNullOrWhiteSpace(requesterLocation.Area))
            {
                return [];
            }

            query = query.Where(d => d.Area == requesterLocation.Area);
        }

        var donors = await query
            .OrderByDescending(d => d.MonthlyPoints)
            .ThenBy(d => d.FullName)
            .Take(top)
            .Select(d => new { d.Id, d.FullName, d.District, d.Area, d.MonthlyPoints })
            .ToListAsync(cancellationToken);

        return donors
            .Select((d, index) => new MonthlyLeaderboardEntryDto(d.Id, d.FullName, d.District, d.Area, d.MonthlyPoints, index + 1))
            .ToList();
    }
}
