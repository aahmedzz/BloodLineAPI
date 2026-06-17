using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Campaigns.Queries.GetCampaignsList;

public record GetCampaignsListQuery(
    int Page = 1,
    int Limit = 10,
    string? Search = null,
    string? Status = null,
    string? City = null
) : IRequest<Result<PaginatedCampaignsResult>>;
