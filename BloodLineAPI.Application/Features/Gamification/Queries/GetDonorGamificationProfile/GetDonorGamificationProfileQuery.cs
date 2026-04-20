using MediatR;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetDonorGamificationProfile;

public sealed record GetDonorGamificationProfileQuery(Guid DonorId)
    : IRequest<DonorGamificationProfileDto>;
