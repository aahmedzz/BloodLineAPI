using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IRegistrationOtpService
{
    Task<Result<string>> GenerateStoreAndSendOTPAsync(User user, CancellationToken cancellationToken);
}
