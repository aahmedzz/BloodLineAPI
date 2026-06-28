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
    DonationCompleted,
    UrgentBloodAppeal,
    General,
    AppointmentRescheduled
}
