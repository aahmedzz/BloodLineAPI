using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Donations.Queries.GetMobileLabResults;

public sealed record LabTestParameterDto(
    string NameAr,
    string NameEn,
    string ResultAr,
    string ResultEn
);

public sealed record ContactOrganizationDto(
    string NameAr,
    string NameEn,
    string PhoneNumber,
    string AddressAr,
    string AddressEn
);

public sealed record FollowUpGuidanceDto(
    string WarningTitleAr,
    string WarningTitleEn,
    string GuidanceMessageAr,
    string GuidanceMessageEn,
    List<ContactOrganizationDto> Contacts
);

public sealed record MobileLabResultResponse(
    Guid DonationId,
    string DonationDate,
    string DonationType,
    string DonationCenterName,
    bool IsSafe,
    string? Notes,
    List<LabTestParameterDto> TestResults,
    FollowUpGuidanceDto? FollowUpGuidance = null
);
