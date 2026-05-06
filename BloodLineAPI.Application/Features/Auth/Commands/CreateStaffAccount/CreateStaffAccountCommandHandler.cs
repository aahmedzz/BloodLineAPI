using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Application.Features.Auth.Commands.CreateStaffAccount;

public sealed class CreateStaffAccountCommandHandler(
    UserManager<User> userManager,
    IApplicationDbContext dbContext,
    ILogger<CreateStaffAccountCommandHandler> logger) : IRequestHandler<CreateStaffAccountCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateStaffAccountCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate the Role is one of the allowed staff roles
        if (request.Role is not "Admin" and not "Doctor" and not "LabDoctor" and not "InventoryManager")
        {
            return Result<Guid>.Failure("Invalid role. Role must be Admin, Doctor, LabDoctor, or InventoryManager.");
        }

        // 2. Check for existing user
        var normalizedUserName = userManager.NormalizeName(request.NationalId);
        var existingUser = await userManager.FindByNameAsync(normalizedUserName);
        if (existingUser != null)
        {
            return Result<Guid>.Failure("A user with this National ID already exists.");
        }

        try
        {
            // 3. Create User entity
            var user = new User
            {
                UserName = request.NationalId,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true, // Trusted creation by Admin
                PhoneNumberConfirmed = true // Trusted creation by Admin
            };

            var createResult = await userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return Result<Guid>.Failure($"Failed to create user: {errors}");
            }

            // 4. Assign role
            var roleResult = await userManager.AddToRoleAsync(user, request.Role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return Result<Guid>.Failure($"Failed to assign role: {errors}");
            }

            // 5. Create Staff entity linked to the user
            var staff = new Staff
            {
                Id = user.Id, // FK to User.Id
                EmployeeIdentifier = $"EMP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                FirstName = request.FirstName,
                SecondName = request.SecondName,
                ThirdName = request.ThirdName,
                FourthName = request.FourthName,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                DepartmentName = request.DepartmentName,
                IsActiveEmployee = true
            };

            dbContext.Staff.Add(staff);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully created staff account {UserId} with role {Role}", user.Id, request.Role);
            return Result<Guid>.Success(user.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while creating staff account for NationalId {NationalId}", request.NationalId);
            return Result<Guid>.Failure("An error occurred while creating the staff account.");
        }
    }
}
