using MediatR;

namespace BloodLineAPI.Application.Features.Dashboard.Queries.GetAdminDashboard;

public sealed record GetAdminDashboardQuery() : IRequest<GetAdminDashboardResult>;
