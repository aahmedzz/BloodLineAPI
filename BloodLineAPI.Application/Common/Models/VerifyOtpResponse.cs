namespace BloodLineAPI.Application.Common.Models;

/// <summary>
/// Response returned after successfully verifying a registration OTP.
/// </summary>
public record VerifyOtpResponse(string TemporaryToken);
