using BloodLineAPI.Application.Features.Appointments.Dtos;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetAppointmentDetails;

public sealed record AppointmentDetailsDto(
    Guid Id,
    DateTime ScheduledDate,
    string StartTime,
    string EndTime,
    string DonationType,
    string Status,
    string? CancellationReason,
    DonationCenterDto Center,
    HealthPreScreeningSummaryDto? HealthScreening);
