using FluentValidation;
using System;

namespace BloodLineAPI.Application.Features.Donors.Commands.UpdateDonor;

public class UpdateDonorCommandValidator : AbstractValidator<UpdateDonorCommand>
{
    public UpdateDonorCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Must(name =>
            {
                if (string.IsNullOrWhiteSpace(name)) return false;
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return parts.Length >= 3;
            }).WithMessage("Full name must contain at least 3 names.")
            .When(x => x.Name != null);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.")
            .When(x => x.Phone != null);

        RuleFor(x => x.BloodType)
            .Must(bt =>
            {
                if (string.IsNullOrWhiteSpace(bt)) return false;
                var normalized = bt.Trim().ToUpperInvariant();
                return normalized is "A+" or "A-" or "B+" or "B-" or "AB+" or "AB-" or "O+" or "O-";
            }).WithMessage("Invalid blood type format. E.g. A+, O-")
            .When(x => !string.IsNullOrWhiteSpace(x.BloodType));

        RuleFor(x => x.Governorate)
            .NotEmpty().WithMessage("Governorate is required.")
            .When(x => x.Governorate != null);

        RuleFor(x => x.District)
            .NotEmpty().WithMessage("District is required.")
            .When(x => x.District != null);

        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("National ID is required.")
            .Matches(@"^\d{14}$").WithMessage("Not a valid National ID.")
            .When(x => x.NationalId != null);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .Must(dob => DateOnly.TryParse(dob, out _)).WithMessage("Invalid date format. Use yyyy-MM-dd.")
            .When(x => x.DateOfBirth != null);
    }
}
