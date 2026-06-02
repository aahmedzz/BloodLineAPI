using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using FluentValidation;

namespace BloodLineAPI.Application.Features.Appointments.Commands.CreateAppointment;

public sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.DonorId).NotEmpty();
        RuleFor(x => x.DonationCenterId).NotEmpty();
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.DonationType)
            .IsInEnum().WithMessage("Invalid donation type.");
        RuleFor(x => x.ScheduledDate)
            .Must(d => d.Date >= dateTimeProvider.LocalNow.Date)
            .WithMessage("Scheduled date cannot be in the past.");
    }
}
