using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Features.Appointments.Commands.UpdateDonationAppointment
{
    public sealed record UpdateDonationAppointmentCommand(
    Guid DonorId,
    Guid AppointmentId,
    DateTime ScheduledDate,
    TimeSpan BookTime,
    string DonationType)
    : IRequest<Result<Unit>>;
}
