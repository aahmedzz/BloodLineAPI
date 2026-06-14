using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.DonorEligibility.Queries.GetEligibleDonors;

public record GetEligibleDonorsQuery(
    int Page = 1,
    int Limit = 10,
    string? Search = null,
    string? BloodType = null,
    string? Status = null,
    string? District = null,
    string? Gender = null) : IRequest<Result<PaginatedEligibilityResult>>;
