using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Dashboard.Queries.GetAdminDashboard;

public sealed record GetAdminDashboardResult(
    string GeneratedAt,
    AdminDashboardSummary Summary,
    IReadOnlyCollection<AdminDashboardInventoryItem> Inventory,
    IReadOnlyCollection<AdminDashboardTrendItem> DonationTrends,
    IReadOnlyCollection<AdminDashboardNotification> Notifications,
    IReadOnlyCollection<AdminDashboardRecentDonor> RecentDonors
);

public sealed record AdminDashboardSummary(
    int TotalDonors,
    int ActiveDonors,
    int CampaignsCount,
    int DoctorsCount,
    int LabWorkersCount,
    int MedicalStaffCount,
    int AvailableBloodUnits,
    int CriticalBloodTypesCount,
    int TotalDonations,
    int CampaignDonations,
    int CampaignDonationsPercentage,
    int WalkInDonations,
    int WalkInDonationsPercentage,
    int AppDonations,
    int AppDonationsPercentage
);

public sealed record AdminDashboardInventoryItem(
    string BloodType,
    int AvailableUnits,
    int MinimumThreshold,
    string Status
);

public sealed record AdminDashboardTrendItem(
    string Month,
    int Donations,
    int NewDonors
);

public sealed record AdminDashboardNotification(
    string Id,
    string Type,
    string Severity,
    string Title,
    string Message
);

public sealed record AdminDashboardRecentDonor(
    string Id,
    string DonorCode,
    string FullName,
    string BloodType,
    string City,
    string LastDonationDate,
    string Status
);
