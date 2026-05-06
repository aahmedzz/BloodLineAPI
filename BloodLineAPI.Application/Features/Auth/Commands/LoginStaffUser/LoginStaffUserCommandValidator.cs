using FluentValidation;

namespace BloodLineAPI.Application.Features.Auth.Commands.LoginStaffUser;

public class LoginStaffUserCommandValidator : AbstractValidator<LoginStaffUserCommand>
{
    public LoginStaffUserCommandValidator()
    {
        RuleFor(v => v.NationalId)
            .NotEmpty().WithMessage("National ID is required.")
            .Length(14).WithMessage("National ID must be 14 digits.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}
