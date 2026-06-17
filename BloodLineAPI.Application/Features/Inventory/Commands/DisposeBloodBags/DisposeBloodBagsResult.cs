using BloodLineAPI.Application.Features.Inventory.Commands.IssueBloodBags;
using BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBags;

namespace BloodLineAPI.Application.Features.Inventory.Commands.DisposeBloodBags;

public sealed record DisposeBloodBagsResult(
    int Processed,
    int Failed,
    List<BagOperationResultItem> Results,
    List<BloodBagDto> UpdatedBags
);
