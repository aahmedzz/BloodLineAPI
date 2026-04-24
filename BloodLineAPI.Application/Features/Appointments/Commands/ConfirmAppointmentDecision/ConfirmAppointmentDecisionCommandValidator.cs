using FluentValidation;

namespace BloodLineAPI.Application.Features.Appointments.Commands.ConfirmAppointmentDecision;

public sealed class ConfirmAppointmentDecisionCommandValidator : AbstractValidator<ConfirmAppointmentDecisionCommand>
{
    public ConfirmAppointmentDecisionCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.DonorId).NotEmpty();
    }
}
