using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.CreateStaffAccount;

public sealed record CreateStaffAccountCommand(
    string NationalId,
    string Password,
    string FirstName,
    string SecondName,
    string ThirdName,
    string? FourthName,
    string? PhoneNumber,
    string? Email,
    string? Address,
    string DepartmentName,
    string Role
) : IRequest<Result<Guid>>;
