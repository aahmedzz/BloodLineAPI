using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace BloodLineAPI.Application.Features.Auth.Commands.ForgotMobilePassword;

public sealed class ForgotMobilePasswordCommandHandler(
    UserManager<User> userManager,
    IWhatsappMessageSender whatsappMessageSender)
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

        var otpCode = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
        user.RegistrationOtpCode = otpCode;
        user.RegistrationOtpExpiryTime = DateTime.UtcNow.AddMinutes(10);

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result<string>.Failure(errors);
        }

        var otpSent = await whatsappMessageSender.SendVerificationOtpAsync(user.PhoneNumber, otpCode, cancellationToken);
        if (!otpSent)
        {
            return Result<string>.Failure("Failed to send verification code. Please try again.");
        }

        return Result<string>.Success("Verification code sent to your WhatsApp number.");
    }
}
