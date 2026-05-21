using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Application.Features.Auth.Commands.DeleteStaff;

public sealed class DeleteStaffCommandHandler(
    UserManager<User> userManager,
    IApplicationDbContext dbContext,
    ILogger<DeleteStaffCommandHandler> logger) : IRequestHandler<DeleteStaffCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteStaffCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch Staff entity
        var staff = await dbContext.Staff
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (staff is null)
        {
            logger.LogWarning("Delete attempt failed: Staff profile not found for ID {StaffId}", request.Id);
            return Result<bool>.Failure("Staff member not found.");
        }

        // 2. Fetch User entity
        var user = await userManager.FindByIdAsync(request.Id.ToString());
        if (user is null || user.IsDeleted)
        {
            logger.LogWarning("Delete attempt failed: User account not found or already deleted for ID {StaffId}", request.Id);
            return Result<bool>.Failure("Staff member not found.");
        }

        // 3. Mark user as deleted and deactivate employee status
        user.IsDeleted = true;
        user.RefreshToken = null; // Instantly revoke any active sessions
        user.RefreshTokenExpiryTime = null;

        staff.IsActiveEmployee = false;

        // 4. Persist User changes through UserManager
        var userUpdateResult = await userManager.UpdateAsync(user);
        if (!userUpdateResult.Succeeded)
        {
            var errors = string.Join(", ", userUpdateResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to soft-delete User {StaffId}: {Errors}", request.Id, errors);
            return Result<bool>.Failure($"Failed to delete user account: {errors}");
        }

        // 5. Save database changes for Staff entity deactivation
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully soft-deleted and deactivated staff member {StaffId}", request.Id);
        return Result<bool>.Success(true);
    }
}
