using BloodLineAPI.Application.Common.Models;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Appointments.Commands.MarkAppointmentNoShow;

public sealed record MarkAppointmentNoShowCommand(
    Guid AppointmentId
) : IRequest<Result<string>>;
