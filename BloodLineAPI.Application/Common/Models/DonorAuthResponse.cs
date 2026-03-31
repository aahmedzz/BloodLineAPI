namespace BloodLineAPI.Application.Common.Models;

public record DonorAuthResponse(
    string Token,
    string RefreshToken,
    AuthenticatedMobileUser User);
