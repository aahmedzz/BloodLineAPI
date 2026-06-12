using System.Collections.Generic;
using BloodLineAPI.Application.Features.Campaigns.Dtos;

namespace BloodLineAPI.Application.Features.Campaigns.Queries.GetCampaignsList;

public record PaginatedCampaignsResult(
    IReadOnlyList<CampaignDto> Data,
    int Total,
    int Page,
    int Limit
);
