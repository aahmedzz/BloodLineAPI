using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Appointments.Dtos;

public record BookingDetailsDto(
    Guid Id,
    string Name,
    string Location,
    string OperatingHours,
    double? AverageRating,
    string CenterType,
    IReadOnlyList<string> AvailableDonationTypes,
    WeeklyGoalDto? WeeklyGoal,         // populated for MainBranch only
    CampaignGoalDto? CampaignGoal      // populated for Campaign only
);

public record WeeklyGoalDto(
    double ProgressPercent,            // e.g. 33.0
    int TotalDonationsThisWeek,
    int TotalTarget
);

public record CampaignGoalDto(
    int TargetDonors,
    int RegisteredDonors,
    double ProgressPercent
);
