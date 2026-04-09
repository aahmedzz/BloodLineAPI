namespace BloodLineAPI.Application.Common.Models;

public record RegisterMobileUserResponse(
    string Message,
    bool RequiresOtpVerification,
    AuthenticatedMobileUser User);
