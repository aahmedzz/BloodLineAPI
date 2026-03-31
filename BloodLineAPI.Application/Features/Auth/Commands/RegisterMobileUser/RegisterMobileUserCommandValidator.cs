using FluentValidation;

namespace BloodLineAPI.Application.Features.Auth.Commands.RegisterMobileUser;

public sealed class RegisterMobileUserCommandValidator : AbstractValidator<RegisterMobileUserCommand>
{
    public RegisterMobileUserCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Must(HaveAtLeastThreeNames).WithMessage("Full name must include at least 3 names.")
            .MaximumLength(400).WithMessage("Full name must not exceed 400 characters.");

        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("National ID is required.")
            .Matches(@"^\d{14}$").WithMessage("Not a valid National ID.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^01[0125][0-9]{8}$").WithMessage("Not a valid mobile number.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }

    private static bool HaveAtLeastThreeNames(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return false;
        }

        var names = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return names.Length >= 3;
    }
}
