using FluentValidation;

namespace BloodLineAPI.Application.Features.Auth.Commands.CreateStaffAccount;

public class CreateStaffAccountCommandValidator : AbstractValidator<CreateStaffAccountCommand>
{
    public CreateStaffAccountCommandValidator()
    {
        RuleFor(v => v.NationalId)
            .NotEmpty().WithMessage("National ID is required.")
            .Length(14).WithMessage("National ID must be 14 digits.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(v => v.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(v => v.SecondName)
            .NotEmpty().WithMessage("Second name is required.")
            .MaximumLength(100).WithMessage("Second name must not exceed 100 characters.");

        RuleFor(v => v.ThirdName)
            .NotEmpty().WithMessage("Third name is required.")
            .MaximumLength(100).WithMessage("Third name must not exceed 100 characters.");

        RuleFor(v => v.FourthName)
            .MaximumLength(100).WithMessage("Fourth name must not exceed 100 characters.");

        RuleFor(v => v.DepartmentName)
            .NotEmpty().WithMessage("Department name is required.")
            .MaximumLength(100).WithMessage("Department name must not exceed 100 characters.");

        RuleFor(v => v.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(r => r is "Admin" or "Doctor" or "LabDoctor" or "InventoryManager")
            .WithMessage("Role must be Admin, Doctor, LabDoctor, or InventoryManager.");
            
        RuleFor(v => v.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(v => !string.IsNullOrEmpty(v.Email));
    }
}
