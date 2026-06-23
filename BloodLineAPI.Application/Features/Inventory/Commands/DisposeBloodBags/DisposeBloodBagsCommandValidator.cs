using FluentValidation;

namespace BloodLineAPI.Application.Features.Inventory.Commands.DisposeBloodBags;

public class DisposeBloodBagsCommandValidator : AbstractValidator<DisposeBloodBagsCommand>
{
    private static readonly string[] ValidReasons =
    {
        "expired", "failed_screening", "damaged_storage",
        "contaminated", "preparation_error", "other"
    };

    public DisposeBloodBagsCommandValidator()
    {
        RuleFor(x => x.BagIds)
            .NotEmpty().WithMessage("يجب تحديد حقيبة دم واحدة على الأقل")
            .Must(ids => ids.All(id => id != Guid.Empty)).WithMessage("معرف الحقيبة غير صالح");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("سبب الإتلاف مطلوب")
            .Must(r => ValidReasons.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage("سبب الإتلاف غير صالح");

        RuleFor(x => x.Notes)
            .NotEmpty().WithMessage("الملاحظات مطلوبة عند اختيار سبب 'أخرى'")
            .When(x => string.Equals(x.Reason, "other", StringComparison.OrdinalIgnoreCase));
    }
}
