using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.DonationCenters.Dtos;

public sealed record WeeklyHourDto(
    int DayOfWeek,
    bool IsClosed,
    string OpeningTime,
    string ClosingTime,
    int? MaxDonorsPerSlot);

public sealed record ExclusionDto(
    Guid Id,
    string Date,
    bool IsClosed,
    string? SpecialOpeningTime,
    string? SpecialClosingTime,
    string Reason);

public sealed record MainBranchSettingsResult(
    Guid Id,
    string Name,
    string Location,
    string AddressDetails,
    string PhoneNumber,
    string Email,
    IReadOnlyList<string> SupportedDonationTypes,
    int SlotDurationMinutes,
    int MaxDonorsPerSlot,
    IReadOnlyList<WeeklyHourDto> WeeklyHours,
    IReadOnlyList<ExclusionDto> Exclusions,
    string UpdatedAt,
    int Version);
