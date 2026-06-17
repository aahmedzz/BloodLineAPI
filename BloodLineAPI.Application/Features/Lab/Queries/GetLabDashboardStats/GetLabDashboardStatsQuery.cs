using MediatR;

namespace BloodLineAPI.Application.Features.Lab.Queries.GetLabDashboardStats;

public sealed record GetLabDashboardStatsQuery : IRequest<GetLabDashboardStatsResult>;
