using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Appointments.Events;

public sealed record SystemAppointmentCancelledEvent(
    Guid AppointmentId,
    Guid CenterId,
    string DonorName,
    TimeSpan StartTime,
    DateTime ScheduledDate,
    string? Reason,
    DateTime? CancelledAt,
    bool IsCancelledByDonor
) : INotification;
