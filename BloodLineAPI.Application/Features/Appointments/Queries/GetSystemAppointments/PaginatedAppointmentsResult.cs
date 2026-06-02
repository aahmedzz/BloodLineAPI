using System.Collections.Generic;
using BloodLineAPI.Application.Features.Appointments.Dtos;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetSystemAppointments;

public record PaginatedAppointmentsResult(
    IReadOnlyList<SystemAppointmentSlotDto> Items,
    int Total,
    int Page,
    int Limit);
