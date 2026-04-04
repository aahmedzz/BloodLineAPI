using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.PrescreeningEligibility;
using BloodLineAPI.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Features.Appointments.Commands.BookDonationAppointment
{
    public sealed record BookDonationAppointmentCommand(
    Guid DonorId,
    Guid DonationCenterId,
    DateTime ScheduledDate,
    TimeSpan BookTime,
    PrescreeningAnswers PrescreeningAnswers,
    string DonationType
   ): IRequest<Result<Guid>>;
}
