using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.CreateStaffAccount;

public sealed record CreateStaffAccountCommand(
    string Name,
    string NationalId,
    string Password,
    string Role,
    string Phone,
    string Address,
    string City,
    string Email
) : IRequest<Result<Guid>>;
