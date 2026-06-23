using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetAdminInventoryDashboard;

public sealed class GetAdminInventoryDashboardQueryHandler : IRequestHandler<GetAdminInventoryDashboardQuery, GetAdminInventoryDashboardResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;


    public GetAdminInventoryDashboardQueryHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<GetAdminInventoryDashboardResult> Handle(GetAdminInventoryDashboardQuery request, CancellationToken cancellationToken)
    {
        var today = _dateTimeProvider.CurrentLocalDate.ToDateTime(TimeOnly.MinValue);

        // 1. Fetch all blood types
        var bloodTypes = await _dbContext.BloodTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // 2. Fetch safety minimum stock thresholds from DB
        var dbThresholds = await _dbContext.BloodStockThresholds
            .AsNoTracking()
            .ToDictionaryAsync(t => t.BloodTypeId ?? 0, t => t.LowThreshold, cancellationToken);

        int GetMinimumThreshold(byte typeId)
        {
            if (dbThresholds.TryGetValue(typeId, out var threshold))
            {
                return threshold;
            }
            return BloodStockThreshold.DefaultThresholds.TryGetValue(typeId, out var def) ? def.Low : 10;
        }

        // 3. Fetch count of available blood bags by blood type
        var availableCountsByType = await _dbContext.BloodBags
            .AsNoTracking()
            .Where(bb => bb.Status == BloodBagStatus.Available && bb.ExpiryDate > today && bb.BloodTypeId != null)
            .GroupBy(bb => bb.BloodTypeId!.Value)
            .Select(g => new { BloodTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BloodTypeId, x => x.Count, cancellationToken);

        // 4. Fetch last update times from transactions
        var latestTransactions = await _dbContext.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.BloodBag.BloodTypeId != null)
            .GroupBy(t => t.BloodBag.BloodTypeId!.Value)
            .Select(g => new { BloodTypeId = g.Key, LastTransactionDate = g.Max(t => t.TransactionDate) })
            .ToDictionaryAsync(x => x.BloodTypeId, x => x.LastTransactionDate, cancellationToken);

        // 5. Fetch last update times from blood bag AuditableEntity dates as a fallback
        var latestBagsMod = await _dbContext.BloodBags
            .AsNoTracking()
            .Where(bb => bb.BloodTypeId != null)
            .GroupBy(bb => bb.BloodTypeId!.Value)
            .Select(g => new { BloodTypeId = g.Key, LastModDate = g.Max(bb => bb.LastModifiedAt ?? bb.CreatedAt) })
            .ToDictionaryAsync(x => x.BloodTypeId, x => x.LastModDate, cancellationToken);

        var inventory = new List<AdminInventoryItemDto>();
        int totalUnits = 0;
        int normalCount = 0;
        int lowCount = 0;
        int criticalCount = 0;
        int outOfStockCount = 0;

        // Order by bloodType Id strictly (1 to 8: A+, A-, B+, B-, AB+, AB-, O+, O-)
        foreach (var bt in bloodTypes.OrderBy(bt => bt.Id))
        {
            var typeName = bt.FullDisplayname;
            var available = availableCountsByType.TryGetValue(bt.Id, out var avCount) ? avCount : 0;
            var minThreshold = GetMinimumThreshold(bt.Id);

            totalUnits += available;

            // Determine status
            string status;
            if (available == 0)
            {
                status = "out_of_stock";
                outOfStockCount++;
            }
            else if (available < (minThreshold * 0.5))
            {
                status = "critical";
                criticalCount++;
            }
            else if (available < minThreshold)
            {
                status = "low";
                lowCount++;
            }
            else
            {
                status = "normal";
                normalCount++;
            }

            // Determine last updated date
            DateTime lastUpdatedDate = _dateTimeProvider.LocalNow; // fallback if no transaction or bag modified
            var dates = new List<DateTime>();
            if (latestTransactions.TryGetValue(bt.Id, out var txDate)) dates.Add(txDate);
            if (latestBagsMod.TryGetValue(bt.Id, out var bagDate)) dates.Add(bagDate);

            if (dates.Count > 0)
            {
                lastUpdatedDate = _dateTimeProvider.ToLocalTime(dates.Max());
            }

            var lastUpdatedStr = lastUpdatedDate.ToString("yyyy-MM-dd");

            inventory.Add(new AdminInventoryItemDto(
                BloodType: typeName,
                AvailableUnits: available,
                MinimumThreshold: minThreshold,
                Status: status,
                LastUpdated: lastUpdatedStr
            ));
        }

        var summary = new AdminInventorySummary(
            TotalUnits: totalUnits,
            NormalCount: normalCount,
            LowCount: lowCount,
            CriticalCount: criticalCount,
            OutOfStockCount: outOfStockCount
        );

        var alerts = inventory
            .Where(item => item.Status != "normal")
            .Select(item => new AdminInventoryAlertDto(
                BloodType: item.BloodType,
                AvailableUnits: item.AvailableUnits,
                MinimumThreshold: item.MinimumThreshold,
                Status: item.Status
            ))
            .ToList();

        // ISO 8601 generatedAt timestamp (UTC format with 'Z' suffix)
        var generatedAt = _dateTimeProvider.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        return new GetAdminInventoryDashboardResult(
            GeneratedAt: generatedAt,
            Summary: summary,
            Inventory: inventory,
            Alerts: alerts
        );
    }
}
