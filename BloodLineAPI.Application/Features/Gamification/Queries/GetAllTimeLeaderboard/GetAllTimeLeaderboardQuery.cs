using MediatR;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetAllTimeLeaderboard;

public sealed record GetAllTimeLeaderboardQuery(
    Guid RequestingDonorId,
    int PageNumber = 1,
    int PageSize = 20,
    bool OnlyMyDistrict = false,
    bool OnlyMyArea = false)
    : IRequest<AllTimeLeaderboardResponseDto>;
