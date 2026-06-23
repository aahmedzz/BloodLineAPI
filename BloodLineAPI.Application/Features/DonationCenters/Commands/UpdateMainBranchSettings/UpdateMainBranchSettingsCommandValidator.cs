using FluentValidation;
using System;
using System.Linq;

namespace BloodLineAPI.Application.Features.DonationCenters.Commands.UpdateMainBranchSettings;

public sealed class UpdateMainBranchSettingsCommandValidator : AbstractValidator<UpdateMainBranchSettingsCommand>
{
    private static readonly int[] AllowedSlotDurations = [15, 30, 45, 60, 90, 120];

    public UpdateMainBranchSettingsCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المركز مطلوب.")
            .MaximumLength(200).WithMessage("اسم المركز يجب ألا يتجاوز 200 حرف.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("الموقع مطلوب.")
            .MaximumLength(300).WithMessage("الموقع يجب ألا يتجاوز 300 حرف.");

        RuleFor(x => x.AddressDetails)
            .NotEmpty().WithMessage("تفاصيل العنوان مطلوبة.")
            .MaximumLength(500).WithMessage("تفاصيل العنوان يجب ألا تتجاوز 500 حرف.");

        RuleFor(x => x.SlotDurationMinutes)
            .Must(x => AllowedSlotDurations.Contains(x))
            .WithMessage("مدة الفترة الزمنية غير صالحة. القيم المسموح بها هي: 15, 30, 45, 60, 90, 120 دقيقة.");

        RuleFor(x => x.MaxDonorsPerSlot)
            .GreaterThan(0).WithMessage("أقصى عدد متبرعين بالفترة يجب أن يكون أكبر من الصفر.");

        RuleFor(x => x.WeeklyHours)
            .Must(hours => hours != null && hours.Count == 7)
            .WithMessage("يجب تحديد ساعات العمل الأسبوعية لكافة أيام الأسبوع السبعة.");

        RuleForEach(x => x.WeeklyHours)
            .ChildRules(hours =>
            {
                hours.RuleFor(h => h.DayOfWeek)
                    .InclusiveBetween(0, 6).WithMessage("اليوم غير صالح.");

                hours.RuleFor(h => h)
                    .Must(h =>
                    {
                        if (h.IsClosed) return true;
                        if (!TimeSpan.TryParse(h.OpeningTime, out var open) || !TimeSpan.TryParse(h.ClosingTime, out var close))
                        {
                            return false;
                        }
                        return open < close;
                    })
                    .WithMessage("وقت الفتح يجب أن يكون قبل وقت الإغلاق للأيام المفتوحة.");
            });

        RuleFor(x => x.Exclusions)
            .Must(ex => ex == null || ex.Select(e => e.Date).Distinct().Count() == ex.Count)
            .WithMessage("يمنع تكرار نفس تاريخ الاستثناء.");

        RuleForEach(x => x.Exclusions)
            .ChildRules(ex =>
            {
                ex.RuleFor(e => e.Date)
                    .NotEmpty().WithMessage("تاريخ الاستثناء مطلوب.")
                    .Must(d => DateTime.TryParse(d, out _)).WithMessage("تاريخ الاستثناء غير صالح.");

                ex.RuleFor(e => e)
                    .Must(e =>
                    {
                        if (e.IsClosed) return true;
                        if (string.IsNullOrEmpty(e.SpecialOpeningTime) || string.IsNullOrEmpty(e.SpecialClosingTime))
                        {
                            return false;
                        }
                        if (!TimeSpan.TryParse(e.SpecialOpeningTime, out var open) || !TimeSpan.TryParse(e.SpecialClosingTime, out var close))
                        {
                            return false;
                        }
                        return open < close;
                    })
                    .WithMessage("وقت الفتح الخاص يجب أن يكون قبل وقت الإغلاق الخاص للأيام المفتوحة.");
            });
    }
}
