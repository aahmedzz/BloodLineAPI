using FluentValidation;
using System;
using System.Linq;

namespace BloodLineAPI.Application.Features.DonationCenters.Commands.UpdateWeeklyBloodTypeTargets
{
    public sealed class UpdateWeeklyBloodTypeTargetsCommandValidator : AbstractValidator<UpdateWeeklyBloodTypeTargetsCommand>
    {
        private static readonly string[] StandardBloodTypes = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];

        public UpdateWeeklyBloodTypeTargetsCommandValidator()
        {
            RuleFor(x => x.CenterId)
                .NotEmpty().WithMessage("معرف المركز مطلوب");

            RuleFor(x => x.Targets)
                .NotEmpty().WithMessage("قائمة الأهداف مطلوبة");

            RuleForEach(x => x.Targets).ChildRules(target =>
            {
                target.RuleFor(t => t.BloodType)
                    .NotEmpty().WithMessage("فصيلة الدم مطلوبة")
                    .Must(bt => StandardBloodTypes.Contains(bt))
                    .WithMessage("فصيلة دم غير صالحة. القيم المسموح بها هي: A+, A-, B+, B-, AB+, AB-, O+, O-");

                target.RuleFor(t => t.TargetCount)
                    .GreaterThanOrEqualTo(0).WithMessage("عدد الفصائل المستهدفة يجب أن يكون أكبر من أو يساوي الصفر");
            });
        }
    }
}
