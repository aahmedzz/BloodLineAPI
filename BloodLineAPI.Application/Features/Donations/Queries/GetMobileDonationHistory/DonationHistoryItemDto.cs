using System;

namespace BloodLineAPI.Application.Features.Donations.Queries.GetMobileDonationHistory;

public sealed record DonationHistoryItemDto(
    Guid Id,
    string DonationType,
    string DonationCenterName,
    string DonationDate,
    bool HasLabResults
);
