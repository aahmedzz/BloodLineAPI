namespace BloodLineAPI.Controllers.V1.Mobile.Requests;

public sealed record CreateAppointmentRequest(
    Guid DonationCenterId,
    DateTime ScheduledDate,
    string StartTime,
    string DonationType);
