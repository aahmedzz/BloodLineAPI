using FluentValidation;

namespace BloodLineAPI.Application.Features.Appointments.Commands.RescheduleAppointment;

public sealed class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.DonorId).NotEmpty();
        RuleFor(x => x.NewScheduledDate)
            .Must(d => d.Date >= DateTime.UtcNow.Date)
            .WithMessage("Reschedule date cannot be in the past.");
    }
}
