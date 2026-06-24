using FluentValidation;

namespace BloodLineAPI.Application.Features.Donors.Commands.UpdateMobileProfile;

public sealed class UpdateMobileProfileCommandValidator : AbstractValidator<UpdateMobileProfileCommand>
{
    public UpdateMobileProfileCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^01[0125][0-9]{8}$")
            .WithMessage("Not a valid mobile number.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(40m, 200m)
            .WithMessage("Weight must be between 40 and 200 kg.")
            .When(x => x.WeightKg.HasValue);

        RuleFor(x => x.Governorate)
            .MaximumLength(100).WithMessage("Governorate must not exceed 100 characters.")
            .When(x => x.Governorate != null);

        RuleFor(x => x.District)
            .MaximumLength(100).WithMessage("District must not exceed 100 characters.")
            .When(x => x.District != null);

        RuleFor(x => x.Area)
            .MaximumLength(100).WithMessage("Area must not exceed 100 characters.")
            .When(x => x.Area != null);
    }
}
