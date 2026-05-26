using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Donations.Queries.GetFilteredDonations;

public record GetFilteredDonationsQuery(
    int Page = 1,
    int Limit = 10,
    string? Search = null,
    string? BloodType = null,
    string? District = null) : IRequest<Result<PaginatedDonationResult>>;
