using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Auth.Commands.ActivateAccount;

public sealed class ActivateAccountCommandHandler(
    UserManager<User> userManager,
    IApplicationDbContext dbContext,
    IRegistrationOtpService registrationOtpService)
    : IRequestHandler<ActivateAccountCommand, Result<ActivateAccountResponse>>
{
    public async Task<Result<ActivateAccountResponse>> Handle(ActivateAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(request.NationalId);
        if (user == null || user.IsDeleted)
        {
            return Result<ActivateAccountResponse>.Failure("National ID not found in our system.");
        }

        if (user.PasswordHash != null)
        {
            return Result<ActivateAccountResponse>.Failure("This account is already activated. Please use the Login screen.");
        }

        // Add the password for the first time
        var addPasswordResult = await userManager.AddPasswordAsync(user, request.Password);
        if (!addPasswordResult.Succeeded)
        {
            var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
            return Result<ActivateAccountResponse>.Failure(errors);
        }

        // Retrieve the corresponding donor details for response payload
        var donor = await dbContext.Donors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == user.Id, cancellationToken);

        if (donor == null)
        {
            return Result<ActivateAccountResponse>.Failure("Donor profile not found in our system.");
        }

        // Send OTP via the registration OTP service (using WhatsApp as configured in that service)
        var otpResult = await registrationOtpService.GenerateStoreAndSendOTPAsync(user, cancellationToken);
        if (!otpResult.IsSuccess)
        {
            return Result<ActivateAccountResponse>.Failure($"Password set, but failed to send verification code. {otpResult.Error}");
        }

        var userPayload = new AuthenticatedMobileUser(
            user.Id,
            user.UserName!,
            user.PhoneNumber!,
            donor.FullName,
            user.PhoneNumberConfirmed,
            donor.IsRegistrationCompleted);

        return Result<ActivateAccountResponse>.Success(
            new ActivateAccountResponse(otpResult.Data!, true, userPayload));
    }
}
