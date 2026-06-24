namespace BloodLineAPI.Application.Common.Models;

public record ActivateAccountResponse(
    string Message,
    bool RequiresOtpVerification,
    AuthenticatedMobileUser User);
