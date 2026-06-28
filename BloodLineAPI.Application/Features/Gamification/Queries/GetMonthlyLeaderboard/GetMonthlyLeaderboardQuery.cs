using MediatR;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetMonthlyLeaderboard;

public sealed record GetMonthlyLeaderboardQuery(
    Guid RequestingDonorId,
    int Top = 10,
    bool OnlyMyDistrict = false,
    bool OnlyMyArea = false)
    : IRequest<MonthlyLeaderboardResponseDto>;
