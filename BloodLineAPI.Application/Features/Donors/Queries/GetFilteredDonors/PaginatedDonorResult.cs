using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Donors.Queries.GetFilteredDonors;

public record PaginatedDonorResult(
    IReadOnlyList<GetAllDonorsDto> Items,
    int Total,
    int Page,
    int Limit);
