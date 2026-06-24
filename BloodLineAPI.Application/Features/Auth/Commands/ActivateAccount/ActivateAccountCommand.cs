using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.ActivateAccount;

public sealed record ActivateAccountCommand(
    string NationalId,
    string Password,
    string ConfirmPassword) : IRequest<Result<ActivateAccountResponse>>;
