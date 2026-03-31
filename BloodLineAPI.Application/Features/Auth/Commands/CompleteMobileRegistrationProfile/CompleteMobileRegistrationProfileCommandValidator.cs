using FluentValidation;

namespace BloodLineAPI.Application.Features.Auth.Commands.CompleteMobileRegistrationProfile;

public sealed class CompleteMobileRegistrationProfileCommandValidator : AbstractValidator<CompleteMobileRegistrationProfileCommand>
{
    public CompleteMobileRegistrationProfileCommandValidator()
    {
        var latestAllowedBirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16));

        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(latestAllowedBirthDate).WithMessage("Donor must be at least 16 years old.")
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-100))).WithMessage("Date of birth is out of accepted range.");

        RuleFor(x => x.WeightKg)
            .GreaterThan(0).When(x => x.WeightKg.HasValue)
            .LessThanOrEqualTo(300).When(x => x.WeightKg.HasValue)
            .WithMessage("Weight must be between 1 and 300 KG.");
    }
}
