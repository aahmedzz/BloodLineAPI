namespace BloodLineAPI.Domain.Enums;

public enum NotificationType
{
    Unknown = 0,
    AppointmentReminder,
    AppointmentConfirmed,
    AppointmentCancelled,
    BadgeEarned,
    PointsEarned,
    RateDonationCenter,
    UrgentBloodAppeal,
    General,
    AppointmentRescheduled,
    DonationReminder,
    BloodBagIssued,
    NewCampaignNearby,
    LabResultsReady
}
