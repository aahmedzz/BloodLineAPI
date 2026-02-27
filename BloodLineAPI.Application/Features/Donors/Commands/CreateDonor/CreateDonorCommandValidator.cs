using FluentValidation;

namespace BloodLineAPI.Application.Features.Donors.Commands.CreateDonor;

public sealed class CreateDonorCommandValidator : AbstractValidator<CreateDonorCommand>
{
    public CreateDonorCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow));

        RuleFor(x => x.BloodType)
            .IsInEnum();
    }
}
