using FluentValidation;

namespace BloodLineAPI.Application.Features.Auth.Commands.ChangeStaffPassword;

public sealed class ChangeStaffPasswordCommandValidator : AbstractValidator<ChangeStaffPasswordCommand>
{
    public ChangeStaffPasswordCommandValidator()
    {
        RuleFor(v => v.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(v => v.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("New password must be at least 6 characters.")
            .NotEqual(v => v.CurrentPassword).WithMessage("New password must be different from current password.");
    }
}
