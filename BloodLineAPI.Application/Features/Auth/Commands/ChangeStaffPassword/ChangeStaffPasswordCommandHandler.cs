using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Application.Features.Auth.Commands.ChangeStaffPassword;

public sealed class ChangeStaffPasswordCommandHandler(
    UserManager<User> userManager,
    ILogger<ChangeStaffPasswordCommandHandler> logger)
    : IRequestHandler<ChangeStaffPasswordCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ChangeStaffPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null || user.IsDeleted)
        {
            return Result<string>.Failure("User not found.");
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Password change failed for user {UserId}: {Errors}", request.UserId, errors);
            return Result<string>.Failure(errors);
        }

        logger.LogInformation("Password changed successfully for staff {UserId}", request.UserId);
        return Result<string>.Success("Password changed successfully.");
    }
}
