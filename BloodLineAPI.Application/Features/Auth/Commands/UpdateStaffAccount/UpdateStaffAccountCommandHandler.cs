using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Application.Features.Auth.Commands.UpdateStaffAccount;

public sealed class UpdateStaffAccountCommandHandler(
    UserManager<User> userManager,
    IApplicationDbContext dbContext,
    ILogger<UpdateStaffAccountCommandHandler> logger) : IRequestHandler<UpdateStaffAccountCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateStaffAccountCommand request, CancellationToken cancellationToken)
    {
        var staff = await dbContext.Staff.FirstOrDefaultAsync(s => s.Id == request.StaffId, cancellationToken);
        if (staff is null)
        {
            return Result<Guid>.Failure("Staff member not found.");
        }

        // 1. Update name if provided (split into parts like create)
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var nameParts = request.Name
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (nameParts.Length < 3)
            {
                return Result<Guid>.Failure("Full name must include at least 3 names.");
            }

            staff.FirstName = nameParts[0];
            staff.SecondName = nameParts[1];
            staff.ThirdName = nameParts[2];
            staff.FourthName = nameParts.Length > 3 ? nameParts[3] : null;
        }

        // 2. Update phone if provided
        if (request.Phone is not null)
        {
            staff.PhoneNumber = request.Phone;

            // Also update the Identity user's phone
            var user = await userManager.FindByIdAsync(request.StaffId.ToString());
            if (user is not null)
            {
                user.PhoneNumber = request.Phone;
                await userManager.UpdateAsync(user);
            }
        }

        // 3. Update address if provided
        if (request.Address is not null)
        {
            staff.Address = request.Address;
        }

        // 4. Update city if provided
        if (request.City is not null)
        {
            staff.City = request.City;
        }

        // 5. Update email if provided
        if (request.Email is not null)
        {
            var user = await userManager.FindByIdAsync(request.StaffId.ToString());
            if (user is not null)
            {
                user.Email = request.Email;
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    return Result<Guid>.Failure($"Failed to update email: {errors}");
                }
            }
        }

        // 6. Update national ID if provided
        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            var user = await userManager.FindByIdAsync(request.StaffId.ToString());
            if (user is not null)
            {
                // Check if the new NationalId is already taken by another user
                var existingUser = await userManager.FindByNameAsync(request.NationalId);
                if (existingUser is not null && existingUser.Id != request.StaffId)
                {
                    return Result<Guid>.Failure("A user with this National ID already exists.");
                }

                user.UserName = request.NationalId;
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    return Result<Guid>.Failure($"Failed to update National ID: {errors}");
                }
            }
        }

        // 7. Update role if provided (also updates DepartmentName)
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (request.Role is not "Admin" and not "Doctor" and not "LabDoctor" and not "InventoryManager")
            {
                return Result<Guid>.Failure("Invalid role. Role must be Admin, Doctor, LabDoctor, or InventoryManager.");
            }

            var user = await userManager.FindByIdAsync(request.StaffId.ToString());
            if (user is not null)
            {
                var currentRoles = await userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                var roleResult = await userManager.AddToRoleAsync(user, request.Role);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    return Result<Guid>.Failure($"Failed to assign role: {errors}");
                }

                staff.DepartmentName = request.Role;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully updated staff account {StaffId}", request.StaffId);
        return Result<Guid>.Success(request.StaffId);
    }
}
