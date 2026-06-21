using MediatR;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetAdminInventoryDashboard;

public sealed record GetAdminInventoryDashboardQuery() : IRequest<GetAdminInventoryDashboardResult>;
