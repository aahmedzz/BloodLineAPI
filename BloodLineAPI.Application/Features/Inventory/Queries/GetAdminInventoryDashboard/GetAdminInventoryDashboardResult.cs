using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetAdminInventoryDashboard;

public sealed record GetAdminInventoryDashboardResult(
    string GeneratedAt,
    AdminInventorySummary Summary,
    IReadOnlyCollection<AdminInventoryItemDto> Inventory,
    IReadOnlyCollection<AdminInventoryAlertDto> Alerts
);

public sealed record AdminInventorySummary(
    int TotalUnits,
    int NormalCount,
    int LowCount,
    int CriticalCount,
    int OutOfStockCount
);

public sealed record AdminInventoryItemDto(
    string BloodType,
    int AvailableUnits,
    int MinimumThreshold,
    string Status,
    string LastUpdated
);

public sealed record AdminInventoryAlertDto(
    string BloodType,
    int AvailableUnits,
    int MinimumThreshold,
    string Status
);
