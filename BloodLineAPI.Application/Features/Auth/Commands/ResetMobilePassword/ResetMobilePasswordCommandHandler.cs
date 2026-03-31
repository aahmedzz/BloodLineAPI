using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace BloodLineAPI.Application.Features.Auth.Commands.ResetMobilePassword;

public sealed class ResetMobilePasswordCommandHandler(UserManager<User> userManager)
    : IRequestHandler<ResetMobilePasswordCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ResetMobilePasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result<string>.Failure("User ID is required.");
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString());

        if (user is null || user.IsDeleted)
        {
            return Result<string>.Failure("User not found.");
        }

        if (!user.PhoneNumberConfirmed)
        {
            return Result<string>.Failure("Please verify your phone number first.");
        }

        var resetResult = await userManager.ResetPasswordAsync(user, request.ResetToken, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
            return Result<string>.Failure(errors);
        }

        user.RegistrationOtpCode = null;
        user.RegistrationOtpExpiryTime = null;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result<string>.Failure(errors);
        }

        return Result<string>.Success("Password reset successfully.");
    }
}
