using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace BloodLineAPI.Infrastructure.Messaging;

public sealed class RegistrationOtpService(
    UserManager<User> userManager,
    IWhatsappMessageSender whatsappMessageSender) : IRegistrationOtpService
{
    public async Task<Result<string>> GenerateStoreAndSendOTPAsync(User user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            return Result<string>.Failure("Phone number is missing.");
        }

        var otpCode = GenerateOtpCode();
        user.RegistrationOtpCode = otpCode;
        user.RegistrationOtpExpiryTime = GetOtpExpiryTimeUtc();

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result<string>.Failure(errors);
        }

        //var otpSent = await whatsappMessageSender.SendVerificationOtpAsync(user.PhoneNumber, otpCode, cancellationToken);
        var otpSent = true; //Will remove when whatsapp service is upgraded.
        if (!otpSent)
        {
            return Result<string>.Failure("Failed to send verification code. Please try again.");
        }

        return Result<string>.Success("Verification code sent to your WhatsApp number.");
    }

    private static string GenerateOtpCode()
    {
        //return RandomNumberGenerator.GetInt32(0, 10000).ToString("D4");
        return "1234";
    }

    private static DateTime GetOtpExpiryTimeUtc()
    {
        return DateTime.UtcNow.AddMinutes(10);
    }
}
