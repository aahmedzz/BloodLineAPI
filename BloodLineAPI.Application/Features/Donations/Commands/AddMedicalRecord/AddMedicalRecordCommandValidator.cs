using BloodLineAPI.Application.Common.Interfaces;
using FluentValidation;
using System;

namespace BloodLineAPI.Application.Features.Donations.Commands.AddMedicalRecord;

public class AddMedicalRecordCommandValidator : AbstractValidator<AddMedicalRecordCommand>
{
    public AddMedicalRecordCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.DonationId)
            .NotEmpty().WithMessage("Donation ID is required.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status =>
            {
                if (string.IsNullOrWhiteSpace(status)) return false;
                return status.Equals("eligible", StringComparison.OrdinalIgnoreCase) ||
                       status.Equals("deferred", StringComparison.OrdinalIgnoreCase) ||
                       status.Equals("ineligible", StringComparison.OrdinalIgnoreCase);
            }).WithMessage("Status must be one of: eligible, deferred, ineligible.");

        RuleFor(x => x.AdditionalData)
            .NotNull().WithMessage("Additional data is required.");

        When(x => x.AdditionalData != null, () =>
        {
            RuleFor(x => x.AdditionalData.Weight)
                .GreaterThan(0).WithMessage("Weight must be greater than 0.");

            RuleFor(x => x.AdditionalData.BloodPressure)
                .NotEmpty().WithMessage("Blood pressure is required.")
                .Matches(@"^\d{2,3}\/\d{2,3}$").WithMessage("Blood pressure must be in the format 'Systolic/Diastolic' (e.g. 120/80).");

            RuleFor(x => x.AdditionalData.Hemoglobin)
                .GreaterThan(0).WithMessage("Hemoglobin level must be greater than 0.");
        });

        RuleFor(x => x.DeferredUntil)
            .Must(date => string.IsNullOrEmpty(date) || DateOnly.TryParse(date, out _))
            .WithMessage("Deferred until must be a valid date in YYYY-MM-DD format.")
            .Must(date =>
            {
                if (string.IsNullOrEmpty(date)) return true;
                if (DateOnly.TryParse(date, out var parsedDate))
                {
                    return parsedDate >= dateTimeProvider.CurrentLocalDate;
                }
                return false;
            })
            .WithMessage("Deferred until date cannot be in the past.");



        RuleFor(x => x.DonationType)
            .NotEmpty().WithMessage("Donation type is required.")
            .Must(dt => dt.Equals("wholeblood", StringComparison.OrdinalIgnoreCase) ||
                        dt.Equals("plasma", StringComparison.OrdinalIgnoreCase) ||
                        dt.Equals("platelets", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Donation type must be wholeblood, plasma, or platelets.");

        RuleFor(x => x.BloodType)
            .Must(bt => string.IsNullOrWhiteSpace(bt) ||
                        bt.Trim().ToUpperInvariant() is "A+" or "A-" or "B+" or "B-" or "AB+" or "AB-" or "O+" or "O-")
            .WithMessage("Invalid blood type format. E.g. A+, O-");
    }
}
