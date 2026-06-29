using FluentValidation;
using System;

namespace BloodLineAPI.Application.Features.DonorEligibility.Queries.GetEmergencyNotificationPreview;

public class GetEmergencyNotificationPreviewQueryValidator : AbstractValidator<GetEmergencyNotificationPreviewQuery>
{
    public GetEmergencyNotificationPreviewQueryValidator()
    {
        RuleFor(x => x.SelectionMode)
            .Must(mode => string.IsNullOrEmpty(mode) || 
                          mode.Equals("selected", StringComparison.OrdinalIgnoreCase) || 
                          mode.Equals("filtered", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SelectionMode must be 'selected' or 'filtered'.");

        // Rules for Selected Mode (default when SelectionMode is null/empty or "selected")
        When(x => string.IsNullOrEmpty(x.SelectionMode) || x.SelectionMode.Equals("selected", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.DonorIds)
                .NotEmpty().WithMessage("DonorIds is required in selected mode.")
                .Must(list => list != null && list.Count > 0).WithMessage("At least one donor must be selected.")
                .Must(list => list != null && list.Count <= 50).WithMessage("Cannot request preview for more than 50 donors at once in selected mode.");

            RuleFor(x => x.Filters)
                .Null().WithMessage("Filters must not be present in selected mode.");

            RuleFor(x => x.ExcludedDonorIds)
                .Null().WithMessage("ExcludedDonorIds must not be present in selected mode.");
        });

        // Rules for Filtered Mode
        When(x => x.SelectionMode != null && x.SelectionMode.Equals("filtered", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.Filters)
                .NotNull().WithMessage("Filters are required in filtered mode.");

            RuleFor(x => x.ExcludedDonorIds)
                .NotNull().WithMessage("ExcludedDonorIds is required in filtered mode (can be empty []).");

            RuleFor(x => x.DonorIds)
                .Null().WithMessage("DonorIds must not be present in filtered mode.");
        });
    }
}
