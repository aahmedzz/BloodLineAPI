using MediatR;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetOutflowHistory;

public sealed record GetOutflowHistoryQuery(
    int Page = 1,
    int Limit = 10,
    string? Search = null,
    string? ActionType = null,
    string? BloodType = null,
    string? PerformedById = null
) : IRequest<GetOutflowHistoryResult>;
