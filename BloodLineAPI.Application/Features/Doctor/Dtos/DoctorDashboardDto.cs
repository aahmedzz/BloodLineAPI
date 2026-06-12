using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Doctor.Dtos;

public record DoctorDashboardDto(
    DashboardStatisticsDto Statistics,
    DonationSourceStatsDto Sources,
    IReadOnlyList<WeeklyDonationChartDto> WeeklyChart,
    IReadOnlyList<ActiveCampaignDto> ActiveCampaigns,
    IReadOnlyList<UpcomingAppointmentDto> UpcomingAppointments,
    IReadOnlyList<RecentDonationDto> RecentDonations
);

public record DashboardStatisticsDto(
    int TodayDonationsCount,
    int TotalDonationsCount,
    int TotalDonorsCount,
    int EligibleDonorsCount,
    int MyActiveCampaignsCount,
    int MyTotalCampaignsCount,
    int MyDonorsCount,
    int MyEligibleDonorsCount
);

public record DonationSourceStatsDto(
    int WalkinTotal,
    int WalkinToday,
    int CampaignTotal,
    int CampaignToday,
    int AppTotal,
    int AppToday
);

public record WeeklyDonationChartDto(
    string DayName,
    string Date,
    int DonationsCount
);

public record ActiveCampaignDto(
    Guid Id,
    string Title,
    string Status,
    int RegisteredDonors,
    int TargetDonors
);

public record UpcomingAppointmentDto(
    Guid Id,
    string Time,
    string DonorName,
    string DonorNationalId,
    string DonorBloodType,
    string Status = "booked"
);

public record RecentDonationDto(
    Guid Id,
    string BloodType,
    string Name,
    string Source,
    string DonationDate
);
