using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Auth.Commands.VerifyActivationOtp;

public sealed class VerifyActivationOtpCommandHandler(
    UserManager<User> userManager,
    IJwtGenerator jwtGenerator,
    IApplicationDbContext dbContext)
    : IRequestHandler<VerifyActivationOtpCommand, Result<DonorAuthResponse>>
{
    public async Task<Result<DonorAuthResponse>> Handle(VerifyActivationOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.NationalId);
        if (user is null || user.IsDeleted)
        {
            return Result<DonorAuthResponse>.Failure("Invalid activation data.");
        }

        if (user.RegistrationOtpExpiryTime is null || user.RegistrationOtpExpiryTime < DateTime.UtcNow)
        {
            return Result<DonorAuthResponse>.Failure("Verification code has expired.");
        }

        if (!string.Equals(user.RegistrationOtpCode, request.OtpCode, StringComparison.Ordinal))
        {
            return Result<DonorAuthResponse>.Failure("Invalid verification code.");
        }

        // OTP verified successfully
        user.PhoneNumberConfirmed = true;
        user.RegistrationOtpCode = null;
        user.RegistrationOtpExpiryTime = null;

        // Retrieve donor profile
        var donor = await dbContext.Donors
            .FirstOrDefaultAsync(d => d.Id == user.Id, cancellationToken);

        if (donor == null)
        {
            return Result<DonorAuthResponse>.Failure("Donor profile not found in our system.");
        }

        // Set registration completed to true since offline donor already has full records
        if (!donor.IsRegistrationCompleted)
        {
            donor.IsRegistrationCompleted = true;
            donor.AddDomainEvent(new BloodLineAPI.Domain.Events.ProfileCompletedEvent(donor.Id, DateTime.UtcNow));
        }

        // Save both User changes and Donor changes
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result<DonorAuthResponse>.Failure(errors);
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);

        // Generate full access tokens
        var refreshToken = jwtGenerator.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user) ?? Enumerable.Empty<string>();
        var accessToken = jwtGenerator.GenerateToken(user, roles);

        var userPayload = new AuthenticatedMobileUser(
            user.Id,
            user.UserName!,
            user.PhoneNumber!,
            donor.FullName,
            user.PhoneNumberConfirmed,
            donor.IsRegistrationCompleted);

        return Result<DonorAuthResponse>.Success(
            new DonorAuthResponse(accessToken, refreshToken, userPayload));
    }
}
