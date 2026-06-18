using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetOutflowHistory;

public sealed record GetOutflowHistoryResult(
    List<OutflowListDto> Items,
    int Page,
    int Limit,
    int Total,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
);
