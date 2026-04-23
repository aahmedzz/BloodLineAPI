namespace BloodLineAPI.Application.Features.Appointments.Dtos;

public sealed record CreateAppointmentResultDto(
    Guid AppointmentId,
    DateTime ScheduledDate,
    string StartTime,
    string EndTime,
    string DonationType,
    string CenterName,
    string Status);
