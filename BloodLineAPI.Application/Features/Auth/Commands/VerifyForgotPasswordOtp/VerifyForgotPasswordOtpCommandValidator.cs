using FluentValidation;

namespace BloodLineAPI.Application.Features.Auth.Commands.VerifyForgotPasswordOtp;

public sealed class VerifyForgotPasswordOtpCommandValidator : AbstractValidator<VerifyForgotPasswordOtpCommand>
{
    public VerifyForgotPasswordOtpCommandValidator()
    {
        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("National ID is required.")
            .Matches(@"^\d{14}$").WithMessage("Not a valid National ID.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("OTP code is required.")
            .Length(4).WithMessage("OTP code must be 4 digits.")
            .Matches(@"^\d{4}$").WithMessage("OTP code must contain digits only.");
    }
}
