using System.Collections.Generic;
using BloodLineAPI.Application.Features.DonationCenters.Dtos;

namespace BloodLineAPI.Controllers.V1.System.Requests;

public sealed record UpdateMainBranchSettingsRequest(
    string Name,
    string Location,
    string AddressDetails,
    List<string> SupportedDonationTypes,
    int SlotDurationMinutes,
    int MaxDonorsPerSlot,
    List<WeeklyHourDto> WeeklyHours,
    List<ExclusionDto> Exclusions,
    int Version);
