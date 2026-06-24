using FluentValidation;

namespace BloodLineAPI.Application.Features.Auth.Commands.ActivateAccount;

public sealed class ActivateAccountCommandValidator : AbstractValidator<ActivateAccountCommand>
{
    public ActivateAccountCommandValidator()
    {
        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("National ID is required.")
            .Matches(@"^\d{14}$").WithMessage("Not a valid National ID.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}
