using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Donations.Queries.GetFilteredDonations;

public record PaginatedDonationResult(
    IReadOnlyList<DonationListDto> Items,
    int Total,
    int Page,
    int Limit);
