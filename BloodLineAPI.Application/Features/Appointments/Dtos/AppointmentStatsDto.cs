namespace BloodLineAPI.Application.Features.Appointments.Dtos;

public sealed record AppointmentStatsDto(
    int Booked,
    int Completed,
    int Missed,
    int Cancelled,
    int Available,
    int Total
);
