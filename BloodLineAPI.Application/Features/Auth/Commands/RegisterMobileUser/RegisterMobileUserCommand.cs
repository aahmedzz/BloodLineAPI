using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Commands.RegisterMobileUser;

public sealed record RegisterMobileUserCommand(
    string FirstName,
    string LastName,
    string NationalId,
    string PhoneNumber,
    string Password,
    string ConfirmPassword) : IRequest<Result<DonorAuthResponse>>;
