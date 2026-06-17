using FluentValidation;
using System;
using System.Linq;

namespace BloodLineAPI.Application.Features.Campaigns.Commands.CreateCampaign;

public sealed class CreateCampaignCommandValidator : AbstractValidator<CreateCampaignCommand>
{
    private static readonly int[] AllowedSlotDurations = [15, 30, 45, 60, 90, 120];

    public CreateCampaignCommandValidator()
    {
        RuleFor(c => c.Title)
            .NotEmpty().WithMessage("العنوان مطلوب")
            .MaximumLength(150).WithMessage("العنوان لا يجب أن يتجاوز 150 حرفاً");

        RuleFor(c => c.City)
            .NotEmpty().WithMessage("المدينة مطلوبة");

        RuleFor(c => c.Latitude)
            .NotNull().WithMessage("خط العرض مطلوب");

        RuleFor(c => c.Longitude)
            .NotNull().WithMessage("خط الطول مطلوب");

        RuleFor(c => c.StartTime)
            .NotEmpty().WithMessage("وقت البدء مطلوب")
            .Matches(@"^(?:[01]\d|2[0-3]):[0-5]\d$").WithMessage("وقت البدء يجب أن يكون بتنسيق HH:mm");

        RuleFor(c => c.EndTime)
            .NotEmpty().WithMessage("وقت النهاية مطلوب")
            .Matches(@"^(?:[01]\d|2[0-3]):[0-5]\d$").WithMessage("وقت النهاية يجب أن يكون بتنسيق HH:mm");

        RuleFor(c => c.SlotDuration)
            .Must(d => AllowedSlotDurations.Contains(d))
            .WithMessage("مدة الجزء غير صالحة. القيم المسموح بها هي: 15, 30, 45, 60, 90, 120 دقيقة");

        RuleFor(c => c.SlotCapacity)
            .InclusiveBetween(1, 20).WithMessage("سعة الجزء يجب أن تكون بين 1 و 20 متبرعاً");

        RuleFor(c => c.TargetDonors)
            .GreaterThanOrEqualTo(1).WithMessage("المتبرعون المستهدفون يجب أن يكونوا على الأقل 1");

        RuleFor(c => c.Description)
            .MaximumLength(500).WithMessage("الوصف لا يجب أن يتجاوز 500 حرفاً");

        RuleFor(c => c.AvailableDonationTypes)
            .NotEmpty().WithMessage("يجب تحديد نوع واحد على الأقل من أنواع التبرع المتاحة")
            .Must(types => types != null && types.Any() && types.All(t => Enum.TryParse<BloodLineAPI.Domain.Enums.DonationType>(t, true, out _)))
            .WithMessage("نوع تبرع غير صالح. القيم المسموح بها هي: WholeBlood, Plasma, Platelets");

        When(c => c.Recurrence != null && c.Recurrence.Enabled, () =>
        {
            RuleFor(c => c.Recurrence!.Type)
                .NotEmpty().WithMessage("نوع التكرار مطلوب عند تفعيل التكرار")
                .Must(t => new[] { "daily", "weekly", "monthly", "custom" }.Contains(t.ToLower()))
                .WithMessage("نوع التكرار غير صالح");

            When(c => c.Recurrence!.Type.Equals("custom", StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(c => c.Recurrence!.WeekDays)
                    .NotEmpty().WithMessage("أيام الأسبوع مطلوبة لتكرار مخصص")
                    .Must(days => days != null && days.All(d => d >= 0 && d <= 6))
                    .WithMessage("قيم أيام الأسبوع يجب أن تكون بين 0 (الأحد) و 6 (السبت)");
            });

            When(c => !string.IsNullOrEmpty(c.Recurrence!.EndDate), () =>
            {
                RuleFor(c => c.Recurrence!.EndDate)
                    .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("تاريخ نهاية التكرار يجب أن يكون بتنسيق YYYY-MM-DD");
            });
        });
    }
}
