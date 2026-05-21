using FluentValidation;

namespace BloodLineAPI.Application.Features.Auth.Commands.UpdateStaffAccount;

public sealed class UpdateStaffAccountCommandValidator : AbstractValidator<UpdateStaffAccountCommand>
{
    public UpdateStaffAccountCommandValidator()
    {
        RuleFor(v => v.StaffId)
            .NotEmpty().WithMessage("Staff ID is required.");

        RuleFor(v => v.Name)
            .MaximumLength(400).WithMessage("Name must not exceed 400 characters.")
            .Must(name =>
            {
                var parts = name?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return parts is { Length: >= 3 };
            }).WithMessage("Full name must include at least 3 names (first, second, and third).")
            .When(v => !string.IsNullOrWhiteSpace(v.Name));

        RuleFor(v => v.NationalId)
            .Length(14).WithMessage("National ID must be 14 digits.")
            .When(v => !string.IsNullOrWhiteSpace(v.NationalId));

        RuleFor(v => v.Role)
            .Must(r => r is "Admin" or "Doctor" or "LabDoctor" or "InventoryManager")
            .WithMessage("Role must be Admin, Doctor, LabDoctor, or InventoryManager.")
            .When(v => !string.IsNullOrWhiteSpace(v.Role));

        RuleFor(v => v.Phone)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
            .When(v => v.Phone is not null);

        RuleFor(v => v.Address)
            .MaximumLength(300).WithMessage("Address must not exceed 300 characters.")
            .When(v => v.Address is not null);

        RuleFor(v => v.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.")
            .When(v => v.City is not null);

        RuleFor(v => v.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(v => !string.IsNullOrEmpty(v.Email));
    }
}
