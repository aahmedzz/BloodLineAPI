using System;

namespace BloodLineAPI.Application.Features.Donations.Queries.GetFilteredDonations;

public record DonationAdditionalData(
    decimal Weight,
    string BloodPressure,
    decimal Hemoglobin);

public record DonationListDto(
    Guid Id,
    string DonationCode,
    Guid DonorId,
    string DonorCode,
    string Name,
    string Gender,
    int Age,
    string NationalId,
    string Phone,
    string Address,
    string District,
    string BloodType,
    string DonationType,
    string Source,
    Guid? CampaignId,
    string? CampaignName,
    string DonationDate,
    bool SentToLab,
    string[] Diseases,
    DonationAdditionalData? AdditionalData,
    bool IsAllergic,
    string Status);
