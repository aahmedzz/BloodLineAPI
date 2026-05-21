using FluentValidation;

namespace BloodLineAPI.Application.Features.Auth.Commands.LoginStaffUser;

public class LoginStaffUserCommandValidator : AbstractValidator<LoginStaffUserCommand>
{
    public LoginStaffUserCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}
