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

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetInventoryDashboard;

public sealed class GetInventoryDashboardQueryHandler : IRequestHandler<GetInventoryDashboardQuery, GetInventoryDashboardResult>
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

    public GetInventoryDashboardQueryHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IOptions<BloodBagExpirySettings> expirySettings)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _expirySettings = expirySettings;
    }

    public async Task<GetInventoryDashboardResult> Handle(GetInventoryDashboardQuery request, CancellationToken cancellationToken)
    {
        var today = _dateTimeProvider.CurrentLocalDate.ToDateTime(TimeOnly.MinValue);
        var warningWindowDays = _expirySettings.Value.WarningWindowDays;
        var expiringSoonThreshold = today.AddDays(warningWindowDays);

        // 1. Fetch counts grouped by status
        var counts = await _dbContext.BloodBags
            .AsNoTracking()
            .GroupBy(bb => bb.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var avGroup = counts.FirstOrDefault(c => c.Status == BloodBagStatus.Available)?.Count ?? 0;
        var expiredGroup = counts.FirstOrDefault(c => c.Status == BloodBagStatus.Expired)?.Count ?? 0;
        var issuedGroup = counts.FirstOrDefault(c => c.Status == BloodBagStatus.Issued)?.Count ?? 0;
        var disposedGroup = counts.FirstOrDefault(c => c.Status == BloodBagStatus.Disposed)?.Count ?? 0;
        var testingGroup = counts.FirstOrDefault(c => c.Status == BloodBagStatus.Testing)?.Count ?? 0;

        // Account for pending expired bags that are still in 'Available' status in DB but past expiry date
        var pendingExpiredCount = await _dbContext.BloodBags
            .CountAsync(bb => bb.Status == BloodBagStatus.Available && bb.ExpiryDate <= today, cancellationToken);

        var availableCount = avGroup - pendingExpiredCount;
        var expiredCount = expiredGroup + pendingExpiredCount;
        var issuedCount = issuedGroup;
        var disposedCount = disposedGroup;
        var testingCount = testingGroup;

        // 2. Fetch expiring soon bags
        var expiringSoonCount = await _dbContext.BloodBags
            .CountAsync(bb => bb.Status == BloodBagStatus.Available && bb.ExpiryDate > today && bb.ExpiryDate <= expiringSoonThreshold, cancellationToken);

        var summary = new DashboardSummary(availableCount, issuedCount, disposedCount, expiringSoonCount, testingCount);

        // Near Expiry Preview (Up to first 3 bags)
        var nearExpiryBagsRaw = await _dbContext.BloodBags
            .AsNoTracking()
            .Include(bb => bb.BloodType)
            .Where(bb => bb.Status == BloodBagStatus.Available && bb.ExpiryDate > today && bb.ExpiryDate <= expiringSoonThreshold)
            .OrderBy(bb => bb.ExpiryDate)
            .Take(3)
            .ToListAsync(cancellationToken);

        var nearExpiryPreview = nearExpiryBagsRaw.Select(bb => new NearExpiryPreviewDto(
            bb.SerialNumber,
            bb.BloodType?.FullDisplayname ?? "Unknown"
        )).ToList();

        var alerts = new DashboardAlerts(expiredCount, expiringSoonCount, nearExpiryPreview);

        // 3. Inventory by Blood Type
        var bloodTypes = await _dbContext.BloodTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dbThresholds = await _dbContext.BloodStockThresholds
            .AsNoTracking()
            .ToDictionaryAsync(t => t.BloodTypeId ?? 0, t => (Low: t.LowThreshold, Critical: t.CriticalThreshold), cancellationToken);

        (int Low, int Critical) GetThreshold(byte typeId)
        {
            if (dbThresholds.TryGetValue(typeId, out var threshold))
            {
                return threshold;
            }
            return DefaultThresholds.TryGetValue(typeId, out var def) ? def : (10, 5);
        }

        var availableCountsByType = await _dbContext.BloodBags
            .AsNoTracking()
            .Where(bb => bb.Status == BloodBagStatus.Available && bb.ExpiryDate > today && bb.BloodTypeId != null)
            .GroupBy(bb => bb.BloodTypeId!.Value)
            .Select(g => new { BloodTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BloodTypeId, x => x.Count, cancellationToken);

        var inventoryByBloodType = new List<BloodTypeStockDto>();

        foreach (var bt in bloodTypes.OrderBy(bt => bt.Id))
        {
            var typeName = bt.FullDisplayname;
            var available = availableCountsByType.TryGetValue(bt.Id, out var avCount) ? avCount : 0;
            var threshold = GetThreshold(bt.Id);

            string statusString;
            if (available == 0)
            {
                statusString = "out_of_stock";
            }
            else if (available <= threshold.Critical)
            {
                statusString = "critical";
            }
            else
            {
                statusString = "normal";
            }

            inventoryByBloodType.Add(new BloodTypeStockDto(typeName, available, statusString, threshold.Low));
        }

        // 4. Indicators Metrics
        var totalBags = availableCount + expiredCount + testingCount; // Active bags in storage
        var totalHandled = availableCount + expiredCount + issuedCount + disposedCount + testingCount;
        var wastePercentage = totalHandled > 0 ? (int)Math.Round((double)(expiredCount + disposedCount) * 100 / totalHandled) : 0;

        var indicators = new DashboardIndicators(totalBags, wastePercentage, testingCount);

        // 5. Recent Activities (Merge latest 5 issuances and discards)
        var latestIssuances = await _dbContext.IssuanceRecords
            .AsNoTracking()
            .Include(ir => ir.BloodBag)
                .ThenInclude(bb => bb.BloodType)
            .Include(ir => ir.IssuedByStaff)
            .OrderByDescending(ir => ir.IssuedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var latestDiscards = await _dbContext.DiscardRecords
            .AsNoTracking()
            .Include(dr => dr.BloodBag)
                .ThenInclude(bb => bb.BloodType)
            .Include(dr => dr.AuthorizedByStaff)
            .OrderByDescending(dr => dr.DiscardDate)
            .Take(5)
            .ToListAsync(cancellationToken);

        // Map to a common format
        var mergedActivities = latestIssuances.Select(ir => new RecentActivityUnionModel
        {
            Id = ir.Id,
            BagCode = ir.BloodBag.SerialNumber,
            BloodType = ir.BloodBag.BloodType?.FullDisplayname ?? "Unknown",
            ActionType = "issued",
            RecipientName = ir.RecipientName,
            PerformedByName = ir.IssuedByStaff.FullName,
            PerformedAt = ir.IssuedAt
        })
        .Concat(latestDiscards.Select(dr => new RecentActivityUnionModel
        {
            Id = dr.Id,
            BagCode = dr.BloodBag.SerialNumber,
            BloodType = dr.BloodBag.BloodType?.FullDisplayname ?? "Unknown",
            ActionType = "disposed",
            RecipientName = null,
            PerformedByName = dr.AuthorizedByStaff.FullName,
            PerformedAt = dr.DiscardDate
        }))
        .OrderByDescending(x => x.PerformedAt)
        .Take(5)
        .ToList();

        var recentActivities = new List<RecentActivityDto>();

        foreach (var act in mergedActivities)
        {
            // Calculate sequential record code OUT-YYYY-NNNN
            var earlierCount = await _dbContext.IssuanceRecords.CountAsync(ir => ir.IssuedAt < act.PerformedAt, cancellationToken)
                + await _dbContext.DiscardRecords.CountAsync(dr => dr.DiscardDate < act.PerformedAt, cancellationToken);

            recentActivities.Add(new RecentActivityDto(
                Id: act.Id.ToString(),
                RecordCode: $"OUT-{act.PerformedAt.Year:D4}-{(earlierCount + 1):D4}",
                BagCode: act.BagCode,
                BloodType: act.BloodType,
                ActionType: act.ActionType,
                RecipientName: act.RecipientName,
                PerformedByName: act.PerformedByName,
                PerformedAt: act.PerformedAt
            ));
        }

        return new GetInventoryDashboardResult(
            summary,
            alerts,
            inventoryByBloodType,
            indicators,
            recentActivities
        );
    }

    private class RecentActivityUnionModel
    {
        public Guid Id { get; set; }
        public string BagCode { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string? RecipientName { get; set; }
        public string PerformedByName { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
    }
}
