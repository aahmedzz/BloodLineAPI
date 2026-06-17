using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBagStats;

public sealed class GetBloodBagStatsQueryHandler : IRequestHandler<GetBloodBagStatsQuery, GetBloodBagStatsResult>
{
    private readonly IApplicationDbContext _dbContext;

    public GetBloodBagStatsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetBloodBagStatsResult> Handle(GetBloodBagStatsQuery request, CancellationToken cancellationToken)
    {
        var counts = await _dbContext.BloodBags
            .AsNoTracking()
            .GroupBy(bb => bb.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new GetBloodBagStatsResult(
            AvailableCount: counts.FirstOrDefault(c => c.Status == BloodBagStatus.Available)?.Count ?? 0,
            ExpiredCount: counts.FirstOrDefault(c => c.Status == BloodBagStatus.Expired)?.Count ?? 0,
            IssuedCount: counts.FirstOrDefault(c => c.Status == BloodBagStatus.Issued)?.Count ?? 0,
            DisposedCount: counts.FirstOrDefault(c => c.Status == BloodBagStatus.Disposed)?.Count ?? 0,
            TestingCount: counts.FirstOrDefault(c => c.Status == BloodBagStatus.Testing)?.Count ?? 0
        );
    }
}
