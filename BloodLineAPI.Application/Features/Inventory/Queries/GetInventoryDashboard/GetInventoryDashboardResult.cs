using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetInventoryDashboard;

public sealed record GetInventoryDashboardResult(
    DashboardSummary Summary,
    DashboardAlerts Alerts,
    IReadOnlyCollection<BloodTypeStockDto> InventoryByBloodType,
    DashboardIndicators Indicators,
    IReadOnlyCollection<RecentActivityDto> RecentActivities
);

public sealed record DashboardSummary(
    int AvailableCount,
    int IssuedCount,
    int DisposedCount,
    int ExpiringSoonCount,
    int TestingCount
);

public sealed record DashboardAlerts(
    int ExpiredCount,
    int NearExpiryCount,
    IReadOnlyCollection<NearExpiryPreviewDto> NearExpiryPreview
);

public sealed record NearExpiryPreviewDto(
    string BagCode,
    string BloodType
);

public sealed record BloodTypeStockDto(
    string BloodType,
    int AvailableUnits,
    string Status,
    int MinimumThreshold
);

public sealed record DashboardIndicators(
    int TotalBags,
    int WastePercentage,
    int TestingCount
);

public sealed record RecentActivityDto(
    string Id,
    string RecordCode,
    string BagCode,
    string BloodType,
    string ActionType,
    string? RecipientName,
    string PerformedByName,
    DateTime PerformedAt
);
