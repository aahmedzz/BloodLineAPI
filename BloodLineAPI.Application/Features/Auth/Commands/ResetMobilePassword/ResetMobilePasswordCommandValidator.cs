using FluentValidation;

namespace BloodLineAPI.Application.Features.Auth.Commands.ResetMobilePassword;

public sealed class ResetMobilePasswordCommandValidator : AbstractValidator<ResetMobilePasswordCommand>
{
    public ResetMobilePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.ResetToken)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}
