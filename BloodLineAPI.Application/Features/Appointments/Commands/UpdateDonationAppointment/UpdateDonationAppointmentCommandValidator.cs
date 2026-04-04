using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Features.Appointments.Commands.UpdateDonationAppointment
{
    public sealed class UpdateDonationAppointmentCommandValidator : AbstractValidator<UpdateDonationAppointmentCommand>
    {
        public UpdateDonationAppointmentCommandValidator()
        {
            RuleFor(x => x.DonorId).NotEmpty();
            RuleFor(x => x.AppointmentId).NotEmpty();
            RuleFor(x => x.DonationType).NotEmpty();
            RuleFor(x => x.ScheduledDate)
           .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
           .WithMessage("Scheduled date must be in the future.");
        }
    }
}
