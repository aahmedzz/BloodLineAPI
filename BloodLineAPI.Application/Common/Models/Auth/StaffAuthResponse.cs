namespace BloodLineAPI.Application.Common.Models.Auth;

/// <summary>
/// Tokens are NOT included in the final JSON response to the client — they're set as HttpOnly cookies by the controller.
/// This record only carries the user payload for the JSON response body.
/// The Token and RefreshToken are included for the handler to generate and pass to the controller,
/// but the controller will strip them into cookies.
/// </summary>
public record StaffAuthResponse(
    string Token,
    string RefreshToken,
    AuthenticatedStaffUser User);
