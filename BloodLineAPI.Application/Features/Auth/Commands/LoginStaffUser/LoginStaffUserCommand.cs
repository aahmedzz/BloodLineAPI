using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.Auth;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.LoginStaffUser;

public sealed record LoginStaffUserCommand(
    string Email,
    string Password
) : IRequest<Result<StaffAuthResponse>>;
