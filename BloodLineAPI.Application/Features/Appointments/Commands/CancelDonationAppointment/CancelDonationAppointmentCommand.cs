using BloodLineAPI.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Features.Appointments.Commands.CancelDonationAppointment
{
    public sealed record CancelDonationAppointmentCommand(Guid DonorId, Guid AppointmentId)
    : IRequest<Result<Unit>>;
}
