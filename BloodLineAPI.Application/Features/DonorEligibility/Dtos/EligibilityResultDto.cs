namespace BloodLineAPI.Application.Features.DonorEligibility.Dtos;

public record EligibilityResultDto(
    string Status,
    int DaysLeft,
    int DaysAgo,
    string EligibleDate);
