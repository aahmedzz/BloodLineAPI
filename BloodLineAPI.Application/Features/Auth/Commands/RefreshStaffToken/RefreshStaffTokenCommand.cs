using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.Auth;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.RefreshStaffToken;

public sealed record RefreshStaffTokenCommand(string Token, string RefreshToken) : IRequest<Result<StaffAuthResponse>>;
