using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Features.Appointments.Queries.DonorAppointments
{
        public sealed class GetDonorAppointmentsQueryValidator : AbstractValidator<GetDonorAppointmentsQuery>
        {
            public GetDonorAppointmentsQueryValidator()
            {
                RuleFor(x => x.DonorId).NotEmpty();
                RuleFor(x => x.Status)
                    .NotEmpty()
                    .Must(f => f.Equals("upcoming", StringComparison.OrdinalIgnoreCase)
                               || f.Equals("past", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("Status must be 'upcoming' or 'past'.");
            }
        }
}
