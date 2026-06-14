using FluentValidation;
using System;

namespace BloodLineAPI.Application.Features.DonorEligibility.Commands.SendEmergencyNotifications;

public class SendEmergencyNotificationsCommandValidator : AbstractValidator<SendEmergencyNotificationsCommand>
{
    public SendEmergencyNotificationsCommandValidator()
    {
        RuleFor(x => x.DonorIds)
            .NotEmpty().WithMessage("DonorIds is required and must contain at least one ID.")
            .Must(list => list != null && list.Count > 0).WithMessage("At least one donor must be selected.")
            .Must(list => list != null && list.Count <= 50).WithMessage("Cannot send to more than 50 donors at once.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required.")
            .Must(type => type != null && type.Equals("emergency", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only 'emergency' notification type is supported.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(320).WithMessage("Message cannot exceed 320 characters.");
    }
}
