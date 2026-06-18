using System;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetOutflowDetail;

public sealed record GetOutflowDetailResult(
    string Id,
    string RecordCode,
    string BagCode,
    string BloodType,
    string DonationType,
    string ActionType,
    string? RecipientName,
    string? NationalId,
    string? Phone,
    string Reason,
    string PerformedById,
    string PerformedByName,
    DateTime PerformedAt
);
