using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Features.Appointments.Commands.BookDonationAppointment
{
    public sealed class BookDonationAppointmentCommandValidator : AbstractValidator<BookDonationAppointmentCommand>
    {
        public BookDonationAppointmentCommandValidator()
        {
            RuleFor(x => x.DonorId).NotEmpty();
            RuleFor(x => x.ScheduledDate).NotEmpty();
            RuleFor(x => x.DonationType).NotEmpty();
            RuleFor(x => x.ScheduledDate)
           .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
           .WithMessage("Scheduled date must be in the future.");
        }
    }
}
