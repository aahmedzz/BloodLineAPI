using MediatR;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetMonthlyLeaderboard;

public sealed record GetMonthlyLeaderboardQuery(
    Guid RequestingDonorId,
    int PageNumber = 1,
    int PageSize = 20,
    bool OnlyMyDistrict = false,
    bool OnlyMyArea = false)
    : IRequest<MonthlyLeaderboardResponseDto>;
