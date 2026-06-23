using FluentValidation;

namespace BloodLineAPI.Application.Features.DonorEligibility.Commands.UpdateCooldownSettings;

public sealed class UpdateCooldownSettingsCommandValidator : AbstractValidator<UpdateCooldownSettingsCommand>
{
    public UpdateCooldownSettingsCommandValidator()
    {
        RuleFor(x => x.WholeBloodMaleDays)
            .InclusiveBetween(1, 365)
            .WithMessage("فترة انتظار الذكور يجب أن تكون بين 1 و 365 يوماً.");

        RuleFor(x => x.WholeBloodFemaleDays)
            .InclusiveBetween(1, 365)
            .WithMessage("فترة انتظار الإناث يجب أن تكون بين 1 و 365 يوماً.");

        RuleFor(x => x.PlasmaDays)
            .GreaterThan(0)
            .WithMessage("فترة انتظار البلازما يجب أن تكون أكبر من الصفر.");

        RuleFor(x => x.PlateletsDays)
            .GreaterThan(0)
            .WithMessage("فترة انتظار الصفائح يجب أن تكون أكبر من الصفر.");

        RuleFor(x => x.DefaultScreeningLockoutDays)
            .GreaterThan(0)
            .WithMessage("فترة الاستبعاد التلقائية يجب أن تكون أكبر من الصفر.");
    }
}
