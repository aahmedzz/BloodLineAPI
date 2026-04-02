namespace BloodLineAPI.Application.Common.Models;

/// <summary>
/// Response returned when login fails but additional data is available
/// (e.g., user needs to complete registration).
/// </summary>
public record LoginFailureResponse(string Message, DonorAuthResponse Data);
