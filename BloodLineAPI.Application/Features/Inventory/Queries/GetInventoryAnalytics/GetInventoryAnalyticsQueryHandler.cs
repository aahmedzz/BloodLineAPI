using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetInventoryAnalytics;

public sealed class GetInventoryAnalyticsQueryHandler : IRequestHandler<GetInventoryAnalyticsQuery, GetInventoryAnalyticsResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IOptions<BloodBagExpirySettings> _expirySettings;

    private static readonly Dictionary<byte, (int Low, int Critical)> DefaultThresholds = new()
    {
        { 1, (10, 5) },  // A+
        { 2, (8, 4) },   // A-
        { 3, (12, 6) },  // B+
        { 4, (10, 5) },  // B-
        { 5, (5, 2) },   // AB+
        { 6, (5, 2) },   // AB-
        { 7, (15, 7) },  // O+
        { 8, (10, 5) }   // O-
    };

    public GetInventoryAnalyticsQueryHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IOptions<BloodBagExpirySettings> expirySettings)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _expirySettings = expirySettings;
    }

    public async Task<GetInventoryAnalyticsResult> Handle(GetInventoryAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var today = _dateTimeProvider.CurrentLocalDate.ToDateTime(TimeOnly.MinValue);
        var warningWindowDays = _expirySettings.Value.WarningWindowDays;
        var expiringSoonThreshold = today.AddDays(warningWindowDays);

        // 1. Fetch live metrics counts
        var availableCount = await _dbContext.BloodBags
            .CountAsync(bb => bb.Status == BloodBagStatus.Available && bb.ExpiryDate > today, cancellationToken);

        var issuedCount = await _dbContext.BloodBags
            .CountAsync(bb => bb.Status == BloodBagStatus.Issued, cancellationToken);

        var expiringSoonCount = await _dbContext.BloodBags
            .CountAsync(bb => bb.Status == BloodBagStatus.Available && bb.ExpiryDate > today && bb.ExpiryDate <= expiringSoonThreshold, cancellationToken);

        var disposedCount = await _dbContext.BloodBags
            .CountAsync(bb => bb.Status == BloodBagStatus.Disposed, cancellationToken);

        var summary = new AnalyticsSummary(availableCount, issuedCount, expiringSoonCount, disposedCount);

        // 2. Fetch blood types and threshold values
        var bloodTypes = await _dbContext.BloodTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dbThresholds = await _dbContext.BloodStockThresholds
            .AsNoTracking()
            .ToDictionaryAsync(t => t.BloodTypeId ?? 0, t => (Low: t.LowThreshold, Critical: t.CriticalThreshold), cancellationToken);

        // Helper to resolve threshold for a given blood type ID
        (int Low, int Critical) GetThreshold(byte typeId)
        {
            if (dbThresholds.TryGetValue(typeId, out var threshold))
            {
                return threshold;
            }
            return DefaultThresholds.TryGetValue(typeId, out var def) ? def : (10, 5);
        }

        // Fetch counts by blood type
        var availableCountsByType = await _dbContext.BloodBags
            .AsNoTracking()
            .Where(bb => bb.Status == BloodBagStatus.Available && bb.ExpiryDate > today && bb.BloodTypeId != null)
            .GroupBy(bb => bb.BloodTypeId!.Value)
            .Select(g => new { BloodTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BloodTypeId, x => x.Count, cancellationToken);

        var issuedCountsByType = await _dbContext.BloodBags
            .AsNoTracking()
            .Where(bb => bb.Status == BloodBagStatus.Issued && bb.BloodTypeId != null)
            .GroupBy(bb => bb.BloodTypeId!.Value)
            .Select(g => new { BloodTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BloodTypeId, x => x.Count, cancellationToken);

        // Build collections per blood type
        var alerts = new List<BloodTypeAlertDto>();
        var inventoryByType = new List<InventoryByBloodTypeDto>();
        var consumptionByType = new List<ConsumptionByBloodTypeDto>();

        foreach (var bt in bloodTypes.OrderBy(bt => bt.Id))
        {
            var typeName = bt.FullDisplayname;
            var available = availableCountsByType.TryGetValue(bt.Id, out var avCount) ? avCount : 0;
            var issued = issuedCountsByType.TryGetValue(bt.Id, out var isCount) ? isCount : 0;
            var threshold = GetThreshold(bt.Id);

            // Determine status
            string alertStatus;
            if (available == 0)
            {
                alertStatus = "out_of_stock";
            }
            else if (available <= threshold.Critical)
            {
                alertStatus = "critical";
            }
            else
            {
                alertStatus = "normal";
            }

            alerts.Add(new BloodTypeAlertDto(typeName, available, threshold.Low, alertStatus));
            inventoryByType.Add(new InventoryByBloodTypeDto(typeName, available, issued, threshold.Low));

            // Consumption calculation
            var totalCount = issued + available;
            var ratio = totalCount > 0 ? (double)issued / totalCount : 0.0;
            var consumptionStatus = ratio > 0.7 ? "high" : "normal";

            consumptionByType.Add(new ConsumptionByBloodTypeDto(typeName, issued, consumptionStatus));
        }

        // 3. Expiring soon bags list
        var expiringBagsRaw = await _dbContext.BloodBags
            .AsNoTracking()
            .Include(bb => bb.BloodType)
            .Where(bb => bb.Status == BloodBagStatus.Available && bb.ExpiryDate > today && bb.ExpiryDate <= expiringSoonThreshold)
            .OrderBy(bb => bb.ExpiryDate)
            .ToListAsync(cancellationToken);

        var expiringSoonBags = expiringBagsRaw.Select(bb =>
        {
            var days = (int)Math.Max(0, (bb.ExpiryDate.Date - today.Date).TotalDays);
            return new ExpiringSoonBagDto(
                bb.Id,
                bb.SerialNumber,
                bb.BloodType?.FullDisplayname ?? "Unknown",
                bb.ExpiryDate.ToString("yyyy-MM-dd"),
                days
            );
        }).ToList();

        // 4. Monthly trends (last 6 months, including current)
        var sixMonthsAgo = today.AddMonths(-5);
        var startOfWindow = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var transactions = await _dbContext.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.TransactionDate >= startOfWindow)
            .ToListAsync(cancellationToken);

        // Issued bags per month (first issue date per bag in the 6 month window)
        var monthlyIssued = transactions
            .Where(t => t.NewStatus == "Issued")
            .GroupBy(t => t.BloodBagId)
            .Select(g => g.Min(t => t.TransactionDate))
            .GroupBy(d => new { d.Year, d.Month })
            .ToDictionary(g => $"{g.Key.Year}-{g.Key.Month:D2}", g => g.Count());

        // Wasted bags per month (first waste date per bag, avoiding double-counting expired -> disposed transitions)
        var monthlyWasted = transactions
            .Where(t => t.NewStatus == "Expired" || (t.NewStatus == "Disposed" && t.PreviousStatus != "Expired"))
            .GroupBy(t => t.BloodBagId)
            .Select(g => g.Min(t => t.TransactionDate))
            .GroupBy(d => new { d.Year, d.Month })
            .ToDictionary(g => $"{g.Key.Year}-{g.Key.Month:D2}", g => g.Count());

        var monthlyTrends = new List<MonthlyTrendDto>();
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = today.AddMonths(-i);
            var monthKey = $"{targetMonth.Year}-{targetMonth.Month:D2}";
            var mIssued = monthlyIssued.TryGetValue(monthKey, out var valIs) ? valIs : 0;
            var mWasted = monthlyWasted.TryGetValue(monthKey, out var valWs) ? valWs : 0;

            monthlyTrends.Add(new MonthlyTrendDto(monthKey, mIssued, mWasted));
        }

        return new GetInventoryAnalyticsResult(
            summary,
            alerts,
            monthlyTrends,
            inventoryByType,
            expiringSoonBags,
            consumptionByType
        );
    }
}
