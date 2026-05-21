namespace BloodLineAPI.Application.Common.Models.Auth;

public record AuthenticatedStaffUser(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string NationalId,
    string Phone,
    string Address,
    string City,
    string Status,
    DateTime CreatedAt);
