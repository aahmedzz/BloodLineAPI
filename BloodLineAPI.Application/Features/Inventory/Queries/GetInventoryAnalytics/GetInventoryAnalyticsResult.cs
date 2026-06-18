using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetInventoryAnalytics;

public sealed record GetInventoryAnalyticsResult(
    AnalyticsSummary Summary,
    IReadOnlyCollection<BloodTypeAlertDto> BloodTypeAlerts,
    IReadOnlyCollection<MonthlyTrendDto> MonthlyTrends,
    IReadOnlyCollection<InventoryByBloodTypeDto> InventoryByBloodType,
    IReadOnlyCollection<ExpiringSoonBagDto> ExpiringSoonBags,
    IReadOnlyCollection<ConsumptionByBloodTypeDto> ConsumptionByBloodType
);

public sealed record AnalyticsSummary(
    int AvailableCount,
    int IssuedCount,
    int ExpiringSoonCount,
    int DisposedCount
);

public sealed record BloodTypeAlertDto(
    string BloodType,
    int AvailableUnits,
    int MinimumThreshold,
    string AlertStatus
);

public sealed record MonthlyTrendDto(
    string Month,
    int Issued,
    int Wasted
);

public sealed record InventoryByBloodTypeDto(
    string BloodType,
    int AvailableUnits,
    int IssuedUnits,
    int MinimumThreshold
);

public sealed record ExpiringSoonBagDto(
    Guid BagId,
    string BagCode,
    string BloodType,
    string ExpiryDate,
    int DaysRemaining
);

public sealed record ConsumptionByBloodTypeDto(
    string BloodType,
    int IssuedUnits,
    string ConsumptionStatus
);
