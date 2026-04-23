using BloodLineAPI.Domain.Enums;
using FluentValidation;

namespace BloodLineAPI.Application.Features.Appointments.Commands.CreateAppointment;

public sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.DonorId).NotEmpty();
        RuleFor(x => x.DonationCenterId).NotEmpty();
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.DonationType)
            .IsInEnum().WithMessage("Invalid donation type.");
        RuleFor(x => x.ScheduledDate)
            .Must(d => d.Date >= DateTime.UtcNow.Date)
            .WithMessage("Scheduled date cannot be in the past.");
    }
}
