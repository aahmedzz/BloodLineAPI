using BloodLineAPI.Domain.Enums;


namespace BloodLineAPI.Application.Common.Models.MobileAppointment
{
    public sealed record AppointmentDetail(
    Guid Id,
    string DonationType,
    DateTime ScheduledDate,
    TimeSpan BookTime,
    AppointmentStatus Status,
    Guid DonationCenterId,
    string CenterName,
    string CenterLocation,
    string CenterAddressDetails);
}

