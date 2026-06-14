using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.DonorEligibility.Dtos;

public record PaginatedEligibilityResult(
    IReadOnlyList<EligibilityDonorDto> Items,
    int Total,
    int Page,
    int Limit,
    int TotalPages);
