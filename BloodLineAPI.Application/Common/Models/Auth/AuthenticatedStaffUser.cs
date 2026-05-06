namespace BloodLineAPI.Application.Common.Models.Auth;

public record AuthenticatedStaffUser(
    Guid UserId,
    string NationalId,
    string FullName,
    string Role,
    string DepartmentName,
    bool IsActiveEmployee);
