using System.Collections.Generic;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonationCenters.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.DonationCenters.Commands.UpdateMainBranchSettings;

public sealed record UpdateMainBranchSettingsCommand(
    string Name,
    string Location,
    string AddressDetails,
    IReadOnlyList<string> SupportedDonationTypes,
    int SlotDurationMinutes,
    int MaxDonorsPerSlot,
    IReadOnlyList<WeeklyHourDto> WeeklyHours,
    IReadOnlyList<ExclusionDto> Exclusions,
    int Version)
    : IRequest<Result<MainBranchSettingsResult>>;
