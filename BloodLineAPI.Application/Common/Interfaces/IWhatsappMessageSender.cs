namespace BloodLineAPI.Application.Common.Interfaces;

public interface IWhatsappMessageSender
{
    Task<bool> SendVerificationOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken);
}
