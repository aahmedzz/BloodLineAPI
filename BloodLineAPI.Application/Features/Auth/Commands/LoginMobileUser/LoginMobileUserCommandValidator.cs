using FluentValidation;
using System.Text.RegularExpressions;

namespace BloodLineAPI.Application.Features.Auth.Commands.LoginMobileUser
{
    public class LoginMobileUserCommandValidator:AbstractValidator<LoginMobileUserCommand>
    {
        public LoginMobileUserCommandValidator()
        {
            RuleFor(x => x.Identifier)
             .NotEmpty().WithMessage("Identifier is required.")
             .Must(id =>
             {
                 if (string.IsNullOrWhiteSpace(id)) return false;
                 var v = id.Trim();
                 if (v.Length == 14 && Regex.IsMatch(v, @"^\d{14}$")) return true;
                 if (v.Length == 11 && Regex.IsMatch(v, @"^01[0125][0-9]{8}$")) return true;
                 return false;
             })
             .WithMessage("Not a valid National ID or mobile number.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
        }
    }
}
