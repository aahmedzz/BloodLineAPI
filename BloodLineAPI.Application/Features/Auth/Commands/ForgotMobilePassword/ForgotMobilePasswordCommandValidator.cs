using FluentValidation;

namespace BloodLineAPI.Application.Features.Auth.Commands.ForgotMobilePassword;

public sealed class ForgotMobilePasswordCommandValidator : AbstractValidator<ForgotMobilePasswordCommand>
{
    public ForgotMobilePasswordCommandValidator()
    {
        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("National ID is required.")
            .Matches(@"^\d{14}$").WithMessage("Not a valid National ID.");
    }
}
