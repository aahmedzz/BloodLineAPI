namespace BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBagStats;

public sealed record GetBloodBagStatsResult(
    int AvailableCount,
    int ExpiredCount,
    int IssuedCount,
    int DisposedCount,
    int TestingCount,
    int ExpiringSoonCount,
    int WastePercentage
);
