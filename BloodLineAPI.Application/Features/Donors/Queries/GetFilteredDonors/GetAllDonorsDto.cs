using System;

namespace BloodLineAPI.Application.Features.Donors.Queries.GetFilteredDonors;

public record GetAllDonorsDto(
    Guid Id,
    string DonorCode,
    string Name,
    string Address,
    string BloodType,
    string? LastDonationDate,
    int DonationsNumber,
    string EligibilityStatus);
