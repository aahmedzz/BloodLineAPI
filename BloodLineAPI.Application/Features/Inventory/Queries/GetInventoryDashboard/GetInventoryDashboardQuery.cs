using MediatR;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetInventoryDashboard;

public sealed record GetInventoryDashboardQuery() : IRequest<GetInventoryDashboardResult>;
