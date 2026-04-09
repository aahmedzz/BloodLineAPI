namespace BloodLineAPI.Application.Common.Models;

public record AuthenticatedMobileUser(
    Guid UserId,
    string NationalId,
    string PhoneNumber,
    string FullName,
    bool IsPhoneNumberVerified,
    bool IsRegistrationCompleted);
