using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;

namespace BloodLineAPI.Application.Features.Inventory.Commands.UpdateInventoryThresholds;

public class UpdateInventoryThresholdsCommandValidator : AbstractValidator<UpdateInventoryThresholdsCommand>
{
    private static readonly HashSet<string> ValidBloodTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"
    };

    public UpdateInventoryThresholdsCommandValidator()
    {
        RuleFor(x => x.Thresholds)
            .NotEmpty().WithMessage("يجب إدخال حدود المخزون")
            .Custom((thresholds, context) =>
            {
                if (thresholds == null) return;

                foreach (var kvp in thresholds)
                {
                    if (!ValidBloodTypes.Contains(kvp.Key))
                    {
                        context.AddFailure($"thresholds.{kvp.Key}", $"فصيلة الدم '{kvp.Key}' غير صالحة");
                    }

                    if (kvp.Value <= 0)
                    {
                        context.AddFailure($"thresholds.{kvp.Key}", "الحد الأدنى للكمية يجب أن يكون رقماً موجباً أكبر من الصفر");
                    }
                }
            });
    }
}
