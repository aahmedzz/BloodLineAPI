using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.DonorEligibility.Dtos;

public record EligibilityStatsDto(
    EligibilityStatusCountsDto StatusCounts,
    Dictionary<string, BloodTypeCountDto> BloodTypeCounts);

public record EligibilityStatusCountsDto(
    int All,
    int Eligible,
    int Soon,
    int NotYet,
    int Deferred,
    int Ineligible);

public record BloodTypeCountDto(
    int Eligible,
    int Total);
