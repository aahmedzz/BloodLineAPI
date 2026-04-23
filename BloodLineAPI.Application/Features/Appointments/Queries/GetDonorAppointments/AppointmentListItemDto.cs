namespace BloodLineAPI.Application.Features.Appointments.Queries.GetDonorAppointments;

public sealed record AppointmentListItemDto(
    Guid Id,
    DateTime ScheduledDate,
    string StartTime,
    string EndTime,
    string DonationType,
    string Status,
    string CenterName,
    string CenterLocation);
