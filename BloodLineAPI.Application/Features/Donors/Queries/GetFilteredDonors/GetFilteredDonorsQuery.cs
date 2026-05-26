using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Donors.Queries.GetFilteredDonors;

public record GetFilteredDonorsQuery(
    int Page = 1,
    int Limit = 10,
    string? Search = null,
    string? BloodType = null,
    string? Status = null,
    string? District = null) : IRequest<Result<PaginatedDonorResult>>;
