using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetInventoryThresholds;

public sealed class GetInventoryThresholdsQueryHandler : IRequestHandler<GetInventoryThresholdsQuery, Dictionary<string, int>>
{
    private readonly IApplicationDbContext _dbContext;

    private static readonly Dictionary<string, int> DefaultThresholds = new()
    {
        { "A+", 10 },
        { "A-", 8 },
        { "B+", 12 },
        { "B-", 10 },
        { "AB+", 5 },
        { "AB-", 5 },
        { "O+", 15 },
        { "O-", 10 }
    };

    public GetInventoryThresholdsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Dictionary<string, int>> Handle(GetInventoryThresholdsQuery request, CancellationToken cancellationToken)
    {
        var bloodTypes = await _dbContext.BloodTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dbThresholds = await _dbContext.BloodStockThresholds
            .AsNoTracking()
            .ToDictionaryAsync(t => t.BloodTypeId ?? 0, t => t.LowThreshold, cancellationToken);

        var result = new Dictionary<string, int>();

        foreach (var bt in bloodTypes.OrderBy(bt => bt.Id))
        {
            var typeName = bt.FullDisplayname;
            if (dbThresholds.TryGetValue(bt.Id, out var val))
            {
                result[typeName] = val;
            }
            else if (DefaultThresholds.TryGetValue(typeName, out var def))
            {
                result[typeName] = def;
            }
            else
            {
                result[typeName] = 10; // safe default fallback
            }
        }

        return result;
    }
}
