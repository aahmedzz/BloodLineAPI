using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace BloodLineAPI.Application.Features.Auth.Commands.ForgotMobilePassword;

public sealed class ForgotMobilePasswordCommandHandler(
    UserManager<User> userManager,
    IRegistrationOtpService registrationOtpService)
    : IRequestHandler<ForgotMobilePasswordCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ForgotMobilePasswordCommand request, CancellationToken cancellationToken)
    {
        var nationalId = request.NationalId?.Trim();
        if (string.IsNullOrWhiteSpace(nationalId))
        {
            return Result<string>.Failure("National ID is required.");
        }

        var user = await userManager.FindByNameAsync(nationalId);

        if (user is null || user.IsDeleted)
        {
            return Result<string>.Failure("User not found.");
        }

        if (!user.PhoneNumberConfirmed)
        {
            return Result<string>.Failure("Please verify your phone number first.");
        }

        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            return Result<string>.Failure("Phone number is missing.");
        }

        return await registrationOtpService.GenerateStoreAndSendOTPAsync(user, cancellationToken);
    }
}
