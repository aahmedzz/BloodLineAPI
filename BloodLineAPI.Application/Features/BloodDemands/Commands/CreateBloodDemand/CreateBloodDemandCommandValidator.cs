using FluentValidation;

namespace BloodLineAPI.Application.Features.BloodDemands.Commands.CreateBloodDemand
{
    public class CreateBloodDemandCommandValidator : AbstractValidator<CreateBloodDemandCommand>
    {
        public CreateBloodDemandCommandValidator()
        {
            RuleFor(x => x.RequesterName)
                .NotEmpty().WithMessage("اسم الجهة الطالبة مطلوب")
                .MinimumLength(3).WithMessage("اسم الجهة الطالبة يجب أن يكون 3 أحرف على الأقل")
                .MaximumLength(200).WithMessage("اسم الجهة الطالبة يجب ألا يتجاوز 200 حرف");

            RuleFor(x => x.RequestedUnits)
                .GreaterThan(0).WithMessage("عدد الوحدات المطلوبة يجب أن يكون أكبر من الصفر");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("الملاحظات يجب ألا تتجاوز 1000 حرف")
                .When(x => !string.IsNullOrEmpty(x.Notes));
        }
    }
}
