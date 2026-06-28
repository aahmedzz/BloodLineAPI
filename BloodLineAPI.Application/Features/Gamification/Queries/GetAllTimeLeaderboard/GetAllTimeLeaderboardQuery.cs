using MediatR;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetAllTimeLeaderboard;

public sealed record GetAllTimeLeaderboardQuery(
    Guid RequestingDonorId,
    int Top = 10,
    bool OnlyMyDistrict = false,
    bool OnlyMyArea = false)
    : IRequest<AllTimeLeaderboardResponseDto>;
