using FluentValidation;

namespace BloodLineAPI.Application.Features.Inventory.Commands.IssueBloodBags;

public class IssueBloodBagsCommandValidator : AbstractValidator<IssueBloodBagsCommand>
{
    public IssueBloodBagsCommandValidator()
    {
        RuleFor(x => x.BagIds)
            .NotEmpty().WithMessage("يجب تحديد حقيبة دم واحدة على الأقل")
            .Must(ids => ids.All(id => id != Guid.Empty)).WithMessage("معرف الحقيبة غير صالح");

        RuleFor(x => x.RecipientName)
            .NotEmpty().WithMessage("اسم المستلم مطلوب")
            .MinimumLength(6).WithMessage("يجب أن يكون اسم المستلم 6 أحرف على الأقل")
            .Matches(@"^[\u0600-\u06FFa-zA-Z\s]+$").WithMessage("اسم المستلم يجب أن يحتوي على أحرف عربية أو إنجليزية ومسافات فقط");

        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("الرقم القومي مطلوب")
            .Matches(@"^\d{14}$").WithMessage("يجب أن يكون الرقم القومي 14 رقم بالضبط");

        RuleFor(x => x.Phone)
            .Matches(@"^01[0125]\d{8}$").WithMessage("رقم الهاتف غير صالح")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("سبب الصرف مطلوب");
    }
}
