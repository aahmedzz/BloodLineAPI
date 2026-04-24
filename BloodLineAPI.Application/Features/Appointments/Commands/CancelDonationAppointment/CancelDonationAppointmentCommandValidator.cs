using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Features.Appointments.Commands.CancelDonationAppointment
{
    public sealed class CancelDonationAppointmentCommandValidator : AbstractValidator<CancelDonationAppointmentCommand>
    {
        public CancelDonationAppointmentCommandValidator()
        {
            RuleFor(x => x.DonorId).NotEmpty();
            RuleFor(x => x.AppointmentId).NotEmpty();
        }
    }
}
