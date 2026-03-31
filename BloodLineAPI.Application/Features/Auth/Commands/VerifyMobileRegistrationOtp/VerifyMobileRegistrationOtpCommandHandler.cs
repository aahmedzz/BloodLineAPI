using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Auth.Commands.VerifyMobileRegistrationOtp;

public sealed class VerifyMobileRegistrationOtpCommandHandler(
    UserManager<User> userManager,
    IApplicationDbContext dbContext,
    IJwtGenerator jwtGenerator)
    : IRequestHandler<VerifyMobileRegistrationOtpCommand, Result<string>>
{
    public async Task<Result<string>> Handle(VerifyMobileRegistrationOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.NationalId);
        if (user is null || user.IsDeleted)
        {
            return Result<string>.Failure("Invalid registration data.");
        }

        if (user.PhoneNumberConfirmed)
        {
            return Result<string>.Failure("Phone number is already verified.");
        }

        if (user.RegistrationOtpExpiryTime is null || user.RegistrationOtpExpiryTime < DateTime.UtcNow)
        {
            return Result<string>.Failure("Verification code has expired.");
        }

        if (!string.Equals(user.RegistrationOtpCode, request.OtpCode, StringComparison.Ordinal))
        {
            return Result<string>.Failure("Invalid verification code.");
        }

        user.PhoneNumberConfirmed = true;
        user.RegistrationOtpCode = null;
        user.RegistrationOtpExpiryTime = null;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result<string>.Failure(errors);
        }

        var temporaryToken = jwtGenerator.GenerateTemporaryToken(user);

        return Result<string>.Success(temporaryToken);
    }
}
