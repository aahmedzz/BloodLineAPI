using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Dashboard.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, GetAdminDashboardResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAdminDashboardQueryHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<GetAdminDashboardResult> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        var localNow = _dateTimeProvider.LocalNow;
        var today = _dateTimeProvider.CurrentLocalDate.ToDateTime(TimeOnly.MinValue);

        // Fetch dashboard statistics sequentially (DbContext is not thread-safe)
        var (totalDonors, activeDonors) = await GetDonorsSummaryAsync(cancellationToken);
        var campaignsCount = await GetCampaignsCountAsync(cancellationToken);
        var (doctorsCount, labWorkersCount) = await GetMedicalStaffBreakdownAsync(cancellationToken);
        var (inventory, availableBloodUnits, criticalBloodTypesCount) = await GetInventoryAndAlertsAsync(today, cancellationToken);
        var donationStats = await GetDonationStatsAsync(cancellationToken);
        var trends = await GetDonationTrendsAsync(localNow, cancellationToken);
        var notifications = await GetNotificationsAsync(inventory, cancellationToken);
        var recentDonors = await GetRecentDonorsAsync(cancellationToken);

        var summary = new AdminDashboardSummary(
            TotalDonors: totalDonors,
            ActiveDonors: activeDonors,
            CampaignsCount: campaignsCount,
            DoctorsCount: doctorsCount,
            LabWorkersCount: labWorkersCount,
            MedicalStaffCount: doctorsCount + labWorkersCount,
            AvailableBloodUnits: availableBloodUnits,
            CriticalBloodTypesCount: criticalBloodTypesCount,
            TotalDonations: donationStats.Total,
            CampaignDonations: donationStats.Campaign,
            CampaignDonationsPercentage: donationStats.CampaignPercentage,
            WalkInDonations: donationStats.WalkIn,
            WalkInDonationsPercentage: donationStats.WalkInPercentage,
            AppDonations: donationStats.App,
            AppDonationsPercentage: donationStats.AppPercentage
        );

        var generatedAt = _dateTimeProvider.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        return new GetAdminDashboardResult(
            GeneratedAt: generatedAt,
            Summary: summary,
            Inventory: inventory,
            DonationTrends: trends,
            Notifications: notifications,
            RecentDonors: recentDonors
        );
    }

    #region Helper Methods

    private async Task<(int Total, int Active)> GetDonorsSummaryAsync(CancellationToken cancellationToken)
    {
        var total = await _dbContext.Donors
            .AsNoTracking()
            .CountAsync(d => !d.User.IsDeleted, cancellationToken);

        var active = await _dbContext.Donors
            .AsNoTracking()
            .CountAsync(d => !d.User.IsDeleted && d.Status == DonorStatus.Eligible, cancellationToken);

        return (total, active);
    }

    private async Task<int> GetCampaignsCountAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.DonationCenters
            .AsNoTracking()
            .CountAsync(c => c.CenterType == CenterType.Campaign, cancellationToken);
    }

    private async Task<(int Doctors, int LabWorkers)> GetMedicalStaffBreakdownAsync(CancellationToken cancellationToken)
    {
        var doctors = await _dbContext.Staff
            .AsNoTracking()
            .CountAsync(s => !s.User.IsDeleted && s.User.UserRoles.Any(ur => ur.Role.Name == "Doctor"), cancellationToken);

        var labWorkers = await _dbContext.Staff
            .AsNoTracking()
            .CountAsync(s => !s.User.IsDeleted && s.User.UserRoles.Any(ur => ur.Role.Name == "LabDoctor"), cancellationToken);

        return (doctors, labWorkers);
    }

    private async Task<(IReadOnlyList<AdminDashboardInventoryItem> Inventory, int TotalUnits, int CriticalCount)> GetInventoryAndAlertsAsync(
        DateTime today,
        CancellationToken cancellationToken)
    {
        var bloodTypes = await _dbContext.BloodTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

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

        var availableCountsByType = await _dbContext.BloodBags
            .AsNoTracking()
            .Where(bb => bb.Status == BloodBagStatus.Available && bb.ExpiryDate > today && bb.BloodTypeId != null)
            .GroupBy(bb => bb.BloodTypeId!.Value)
            .Select(g => new { BloodTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BloodTypeId, x => x.Count, cancellationToken);

        var inventory = new List<AdminDashboardInventoryItem>();
        int totalUnits = 0;
        int criticalCount = 0;

        foreach (var bt in bloodTypes.OrderBy(bt => bt.Id))
        {
            var typeName = bt.FullDisplayname;
            var available = availableCountsByType.TryGetValue(bt.Id, out var avCount) ? avCount : 0;
            var minThreshold = GetMinimumThreshold(bt.Id);

            totalUnits += available;

            string status;
            if (available == 0)
            {
                status = "out_of_stock";
                criticalCount++;
            }
            else if (available < (minThreshold * 0.5))
            {
                status = "critical";
                criticalCount++;
            }
            else if (available < minThreshold)
            {
                status = "low";
            }
            else
            {
                status = "normal";
            }

            inventory.Add(new AdminDashboardInventoryItem(
                BloodType: typeName,
                AvailableUnits: available,
                MinimumThreshold: minThreshold,
                Status: status
            ));
        }

        return (inventory, totalUnits, criticalCount);
    }

    private async Task<DonationStatsHelper> GetDonationStatsAsync(CancellationToken cancellationToken)
    {
        var completedDonations = await _dbContext.DonationAppointments
            .AsNoTracking()
            .Include(da => da.DonationCenter)
            .Where(da => da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved)
            .Select(da => new { da.Source, CenterType = da.DonationCenter.CenterType })
            .ToListAsync(cancellationToken);

        var campaign = completedDonations
            .Count(x => x.CenterType == CenterType.Campaign || x.Source == DonationSource.Campaign);

        var walkIn = completedDonations
            .Count(x => x.CenterType != CenterType.Campaign && x.Source != DonationSource.Campaign);

        var app = completedDonations
            .Count(x => x.Source == DonationSource.MobileApp);

        var total = campaign + walkIn + app;

        int campaignPct = 0;
        int walkInPct = 0;
        int appPct = 0;

        if (total > 0)
        {
            campaignPct = (int)Math.Round((double)campaign * 100 / total);
            walkInPct = (int)Math.Round((double)walkIn * 100 / total);
            appPct = 100 - (campaignPct + walkInPct);
        }

        return new DonationStatsHelper(total, campaign, campaignPct, walkIn, walkInPct, app, appPct);
    }

    private async Task<IReadOnlyList<AdminDashboardTrendItem>> GetDonationTrendsAsync(DateTime localNow, CancellationToken cancellationToken)
    {
        var trends = new List<AdminDashboardTrendItem>();
        for (int i = 5; i >= 0; i--)
        {
            var mDate = localNow.AddMonths(-i);
            var monthKey = mDate.ToString("yyyy-MM");
            var firstDayOfMonth = new DateTime(mDate.Year, mDate.Month, 1);
            var nextMonthFirstDay = firstDayOfMonth.AddMonths(1);

            var donationsCount = await _dbContext.DonationAppointments
                .AsNoTracking()
                .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                                  da.ScheduledDate >= firstDayOfMonth &&
                                  da.ScheduledDate < nextMonthFirstDay, cancellationToken);

            var newDonorsCount = await _dbContext.Donors
                .AsNoTracking()
                .CountAsync(d => d.CreatedAt >= firstDayOfMonth &&
                                 d.CreatedAt < nextMonthFirstDay, cancellationToken);

            trends.Add(new AdminDashboardTrendItem(
                Month: monthKey,
                Donations: donationsCount,
                NewDonors: newDonorsCount
            ));
        }

        return trends;
    }

    private async Task<IReadOnlyList<AdminDashboardNotification>> GetNotificationsAsync(
        IReadOnlyList<AdminDashboardInventoryItem> inventory,
        CancellationToken cancellationToken)
    {
        var notifications = new List<AdminDashboardNotification>();
        int alertIndex = 1;

        foreach (var item in inventory)
        {
            if (item.Status == "critical" || item.Status == "out_of_stock")
            {
                notifications.Add(new AdminDashboardNotification(
                    Id: $"alert-{alertIndex++}",
                    Type: "inventory",
                    Severity: "critical",
                    Title: "نقص حرج في الفصائل",
                    Message: $"مخزون الفصيلة {item.BloodType} أقل من الحد الأدنى المسموح به."
                ));
            }
            else if (item.Status == "low")
            {
                notifications.Add(new AdminDashboardNotification(
                    Id: $"alert-{alertIndex++}",
                    Type: "inventory",
                    Severity: "warning",
                    Title: $"مخزون منخفض بالفصيلة {item.BloodType}",
                    Message: $"مخزون الفصيلة {item.BloodType} يقترب من المستويات الحرجة."
                ));
            }
        }

        var activeAppeals = await _dbContext.UrgentBloodAppeals
            .AsNoTracking()
            .Where(a => a.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var appeal in activeAppeals)
        {
            notifications.Add(new AdminDashboardNotification(
                Id: $"appeal-{appeal.Id}",
                Type: "system",
                Severity: "info",
                Title: appeal.Title,
                Message: appeal.Description
            ));
        }

        return notifications
            .OrderBy(n => n.Severity == "critical" ? 0 : n.Severity == "warning" ? 1 : 2)
            .ToList();
    }

    private async Task<IReadOnlyList<AdminDashboardRecentDonor>> GetRecentDonorsAsync(CancellationToken cancellationToken)
    {
        var recentDonorsDb = await _dbContext.Donors
            .AsNoTracking()
            .Include(d => d.BloodType)
            .Where(d => d.LastDonationDate != null)
            .OrderByDescending(d => d.LastDonationDate)
            .Take(5)
            .ToListAsync(cancellationToken);

        return recentDonorsDb.Select(d => new AdminDashboardRecentDonor(
            Id: d.Id.ToString(),
            DonorCode: d.DonorCode,
            FullName: d.FullName,
            BloodType: d.BloodType?.FullDisplayname ?? string.Empty,
            City: d.District ?? d.Governorate ?? string.Empty,
            LastDonationDate: d.LastDonationDate.HasValue ? d.LastDonationDate.Value.ToString("yyyy-MM-dd") : string.Empty,
            Status: d.Status.ToString().ToLowerInvariant()
        )).ToList();
    }

    #endregion

    private sealed record DonationStatsHelper(
        int Total,
        int Campaign,
        int CampaignPercentage,
        int WalkIn,
        int WalkInPercentage,
        int App,
        int AppPercentage
    );
}
