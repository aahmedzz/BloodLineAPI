using FluentValidation;

namespace BloodLineAPI.Application.Features.Appointments.Commands.SubmitHealthPreScreening;

public sealed class SubmitHealthPreScreeningCommandValidator : AbstractValidator<SubmitHealthPreScreeningCommand>
{
    public SubmitHealthPreScreeningCommandValidator()
    {
        RuleFor(x => x.DonorId)
            .NotEmpty().WithMessage("Donor is required.");

        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("Appointment is required.");
    }
}
