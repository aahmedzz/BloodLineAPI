using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBagStats;

public sealed class GetBloodBagStatsQueryHandler : IRequestHandler<GetBloodBagStatsQuery, GetBloodBagStatsResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetBloodBagStatsQueryHandler(IApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<GetBloodBagStatsResult> Handle(GetBloodBagStatsQuery request, CancellationToken cancellationToken)
    {
        var counts = await _dbContext.BloodBags
            .AsNoTracking()
            .GroupBy(bb => bb.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var today = _dateTimeProvider.CurrentLocalDate.ToDateTime(TimeOnly.MinValue);
        var expiringSoonThreshold = today.AddDays(7);

        var expiringSoonCount = await _dbContext.BloodBags
            .AsNoTracking()
            .CountAsync(bb => bb.Status == BloodBagStatus.Available && bb.ExpiryDate > today && bb.ExpiryDate <= expiringSoonThreshold, cancellationToken);

        var available = counts.FirstOrDefault(c => c.Status == BloodBagStatus.Available)?.Count ?? 0;
        var expired = counts.FirstOrDefault(c => c.Status == BloodBagStatus.Expired)?.Count ?? 0;
        var issued = counts.FirstOrDefault(c => c.Status == BloodBagStatus.Issued)?.Count ?? 0;
        var disposed = counts.FirstOrDefault(c => c.Status == BloodBagStatus.Disposed)?.Count ?? 0;
        var testing = counts.FirstOrDefault(c => c.Status == BloodBagStatus.Testing)?.Count ?? 0;

        var total = available + expired + issued + disposed + testing;
        var wastePercentage = total > 0 ? (int)Math.Round((double)(expired + disposed) * 100 / total) : 0;

        return new GetBloodBagStatsResult(
            AvailableCount: available,
            ExpiredCount: expired,
            IssuedCount: issued,
            DisposedCount: disposed,
            TestingCount: testing,
            ExpiringSoonCount: expiringSoonCount,
            WastePercentage: wastePercentage
        );
    }
}
