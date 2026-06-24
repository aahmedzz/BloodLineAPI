using FluentValidation;

namespace BloodLineAPI.Application.Features.Donors.Commands.UpdateDonorLocation;

public sealed class UpdateDonorLocationCommandValidator : AbstractValidator<UpdateDonorLocationCommand>
{
    public UpdateDonorLocationCommandValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0)
            .WithMessage("Longitude must be between -180 and 180.");
    }
}
