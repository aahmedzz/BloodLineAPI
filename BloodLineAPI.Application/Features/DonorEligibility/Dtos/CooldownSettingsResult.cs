namespace BloodLineAPI.Application.Features.DonorEligibility.Dtos;

public sealed record CooldownSettingsResult(
    int WholeBloodMaleDays,
    int WholeBloodFemaleDays,
    int PlasmaDays,
    int PlateletsDays,
    int DefaultScreeningLockoutDays);
