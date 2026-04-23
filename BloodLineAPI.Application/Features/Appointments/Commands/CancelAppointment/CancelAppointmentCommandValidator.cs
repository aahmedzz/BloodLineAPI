using FluentValidation;

namespace BloodLineAPI.Application.Features.Appointments.Commands.CancelAppointment;

public sealed class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.DonorId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
