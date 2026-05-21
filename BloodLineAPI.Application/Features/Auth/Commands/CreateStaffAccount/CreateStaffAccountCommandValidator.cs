using FluentValidation;

namespace BloodLineAPI.Application.Features.Auth.Commands.CreateStaffAccount;

public class CreateStaffAccountCommandValidator : AbstractValidator<CreateStaffAccountCommand>
{
    public CreateStaffAccountCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(400).WithMessage("Name must not exceed 400 characters.")
            .Must(name =>
            {
                var parts = name?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return parts is { Length: >= 3 };
            }).WithMessage("Full name must include at least 3 names (first, second, and third).");

        RuleFor(v => v.NationalId)
            .NotEmpty().WithMessage("National ID is required.")
            .Length(14).WithMessage("National ID must be 14 digits.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(v => v.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(r => r is "Admin" or "Doctor" or "LabDoctor" or "InventoryManager")
            .WithMessage("Role must be Admin, Doctor, LabDoctor, or InventoryManager.");

        RuleFor(v => v.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");

        RuleFor(v => v.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(300).WithMessage("Address must not exceed 300 characters.");

        RuleFor(v => v.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
    }
}
