using BloodLineAPI.Application.Common.Models;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Appointments.Commands.SystemCancelAppointment;

public sealed record SystemCancelAppointmentCommand(
    Guid AppointmentId,
    string? Reason
) : IRequest<Result<string>>;
