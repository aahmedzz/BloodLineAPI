namespace BloodLineAPI.Application.Common.Models;

/// <summary>
/// Response returned after successfully verifying a forgot-password OTP.
/// </summary>
public record VerifyResetOtpResponse(string UserId, string ResetToken);
