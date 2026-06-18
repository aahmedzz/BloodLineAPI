using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Inventory.Commands.UpdateInventoryThresholds;

public sealed class UpdateInventoryThresholdsCommandHandler : IRequestHandler<UpdateInventoryThresholdsCommand, Dictionary<string, int>>
{
    private readonly IApplicationDbContext _dbContext;

    public UpdateInventoryThresholdsCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Dictionary<string, int>> Handle(UpdateInventoryThresholdsCommand request, CancellationToken cancellationToken)
    {
        var bloodTypes = await _dbContext.BloodTypes
            .ToListAsync(cancellationToken);

        var dbThresholds = await _dbContext.BloodStockThresholds
            .ToListAsync(cancellationToken);

        foreach (var kvp in request.Thresholds)
        {
            var bt = bloodTypes.FirstOrDefault(x => string.Equals(x.FullDisplayname, kvp.Key, StringComparison.OrdinalIgnoreCase));
            if (bt == null) continue;

            var dbThreshold = dbThresholds.FirstOrDefault(t => t.BloodTypeId == bt.Id);
            var newLow = kvp.Value;
            var newCritical = (int)Math.Round(newLow * 0.5);

            if (dbThreshold != null)
            {
                dbThreshold.LowThreshold = newLow;
                dbThreshold.CriticalThreshold = newCritical;
            }
            else
            {
                var threshold = new BloodStockThreshold
                {
                    Id = Guid.NewGuid(),
                    BloodTypeId = bt.Id,
                    LowThreshold = newLow,
                    CriticalThreshold = newCritical
                };
                _dbContext.BloodStockThresholds.Add(threshold);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Fetch and return the updated thresholds in same format
        var updatedDbThresholds = await _dbContext.BloodStockThresholds
            .AsNoTracking()
            .ToDictionaryAsync(t => t.BloodTypeId ?? 0, t => t.LowThreshold, cancellationToken);

        var result = new Dictionary<string, int>();
        foreach (var bt in bloodTypes.OrderBy(bt => bt.Id))
        {
            var typeName = bt.FullDisplayname;
            if (updatedDbThresholds.TryGetValue(bt.Id, out var val))
            {
                result[typeName] = val;
            }
            else
            {
                result[typeName] = 10; // default fallback if somehow not set
            }
        }

        return result;
    }
}
