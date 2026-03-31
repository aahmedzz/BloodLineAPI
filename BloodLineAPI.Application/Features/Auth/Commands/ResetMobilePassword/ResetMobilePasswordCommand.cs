using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.ResetMobilePassword;

public sealed record ResetMobilePasswordCommand(
    Guid UserId,
    string ResetToken,
    string NewPassword,
    string ConfirmPassword) : IRequest<Result<string>>;
