using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.DonorEligibility.Queries.GetEligibilityStats;

public record GetEligibilityStatsQuery : IRequest<Result<EligibilityStatsDto>>;
