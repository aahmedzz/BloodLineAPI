using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.ForgotMobilePassword;

public sealed record ForgotMobilePasswordCommand(string NationalId) : IRequest<Result<string>>;
