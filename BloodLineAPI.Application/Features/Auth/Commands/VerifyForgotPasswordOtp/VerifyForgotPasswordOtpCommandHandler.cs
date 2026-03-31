using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace BloodLineAPI.Application.Features.Auth.Commands.VerifyForgotPasswordOtp;

public sealed class VerifyForgotPasswordOtpCommandHandler(UserManager<User> userManager)
    : IRequestHandler<VerifyForgotPasswordOtpCommand, Result<string>>
{
    public async Task<Result<string>> Handle(VerifyForgotPasswordOtpCommand request, CancellationToken cancellationToken)
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

        if (user.RegistrationOtpExpiryTime is null || user.RegistrationOtpExpiryTime < DateTime.UtcNow)
        {
            return Result<string>.Failure("Verification code has expired.");
        }

        if (!string.Equals(user.RegistrationOtpCode, request.OtpCode, StringComparison.Ordinal))
        {
            return Result<string>.Failure("Invalid verification code.");
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        user.RegistrationOtpCode = null;
        user.RegistrationOtpExpiryTime = null;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result<string>.Failure(errors);
        }

        return Result<string>.Success($"{user.Id}|{resetToken}");
    }
}
