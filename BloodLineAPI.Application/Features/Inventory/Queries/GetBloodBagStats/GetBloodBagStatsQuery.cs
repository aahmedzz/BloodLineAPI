using MediatR;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBagStats;

public sealed record GetBloodBagStatsQuery() : IRequest<GetBloodBagStatsResult>;
