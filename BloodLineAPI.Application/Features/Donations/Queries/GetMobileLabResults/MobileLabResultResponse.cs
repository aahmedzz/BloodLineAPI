using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Donations.Queries.GetMobileLabResults;

public sealed record LabTestParameterDto(
    string Name,
    string Result
);

public sealed record MobileLabResultResponse(
    Guid DonationId,
    string DonationDate,
    string DonationType,
    string DonationCenterName,
    bool IsSafe,
    string? Notes,
    List<LabTestParameterDto> TestResults
);
