using BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBags;

namespace BloodLineAPI.Application.Features.Inventory.Commands.IssueBloodBags;

public sealed record BagOperationResultItem(
    Guid BagId,
    bool Success,
    string? ErrorCode = null,
    string? Error = null
);

public sealed record IssueBloodBagsResult(
    int Processed,
    int Failed,
    List<BagOperationResultItem> Results,
    List<BloodBagDto> UpdatedBags
);
