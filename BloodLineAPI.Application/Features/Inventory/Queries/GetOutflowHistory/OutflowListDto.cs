using System;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetOutflowHistory;

public sealed record OutflowListDto(
    string Id,
    string RecordCode,
    string BagCode,
    string BloodType,
    string DonationType,
    string ActionType,
    string? RecipientName,
    string PerformedByName,
    DateTime PerformedAt
);
