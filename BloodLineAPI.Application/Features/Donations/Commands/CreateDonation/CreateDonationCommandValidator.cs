using FluentValidation;
using System;

namespace BloodLineAPI.Application.Features.Donations.Commands.CreateDonation;

public class CreateDonationCommandValidator : AbstractValidator<CreateDonationCommand>
{
    public CreateDonationCommandValidator()
    {
        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("National ID is required.")
            .Length(14).WithMessage("National ID must be exactly 14 digits.")
            .Matches(@"^\d{14}$").WithMessage("National ID must be numeric.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Must(name =>
            {
                if (string.IsNullOrWhiteSpace(name)) return false;
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return parts.Length >= 3;
            }).WithMessage("Full name must contain at least 3 names.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .Must(g => g.Equals("male", StringComparison.OrdinalIgnoreCase) || 
                       g.Equals("female", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Gender must be male or female.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .Must(dob => DateOnly.TryParse(dob, out _))
            .WithMessage("Date of birth must be a valid date in YYYY-MM-DD format.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");

        RuleFor(x => x.Governorate)
            .NotEmpty().WithMessage("Governorate is required.");

        RuleFor(x => x.District)
            .NotEmpty().WithMessage("District is required.");

        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Source is required.")
            .Must(src => src.Equals("walkin", StringComparison.OrdinalIgnoreCase) ||
                         src.Equals("campaign", StringComparison.OrdinalIgnoreCase) ||
                         src.Equals("mobileapp", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Source must be walkin, campaign, or mobileapp.");

        RuleFor(x => x.DonationCenterId)
            .NotEmpty().WithMessage("Donation center is required.");
    }
}
