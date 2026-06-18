using MediatR;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetInventoryAnalytics;

public sealed record GetInventoryAnalyticsQuery() : IRequest<GetInventoryAnalyticsResult>;
