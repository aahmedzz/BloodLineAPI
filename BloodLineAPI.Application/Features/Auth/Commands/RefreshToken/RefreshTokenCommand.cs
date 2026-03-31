using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string Token,
    string RefreshToken) : IRequest<Result<DonorAuthResponse>>;
