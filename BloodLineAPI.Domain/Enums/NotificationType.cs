namespace BloodLineAPI.Domain.Enums;

public enum NotificationType
{
    Unknown = 0,
    AppointmentReminder,
    AppointmentConfirmed,
    AppointmentCancelled,
    BadgeEarned,
    RateDonationCenter,
    DonationCompleted,
    UrgentBloodAppeal,
    General,
    AppointmentRescheduled
}
