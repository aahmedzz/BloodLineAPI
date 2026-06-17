using System;

namespace BloodLineAPI.Application.Features.DonorEligibility.Dtos;

public record EligibilityDonorDto(
    Guid Id,
    string DonorCode,
    string Name,
    string Gender,
    int Age,
    string NationalId,
    string Phone,
    string Address,
    string District,
    string? Governorate,
    string? Area,
    string? DateOfBirth,
    string? BloodType,
    string Status,
    string? DeferredUntil,
    string? LastDonationDate,
    int Donations,
    bool HasAppAccount,
    EligibilityResultDto Eligibility);
