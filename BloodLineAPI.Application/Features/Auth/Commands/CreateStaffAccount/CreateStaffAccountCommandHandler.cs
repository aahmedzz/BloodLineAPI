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

        // 2. Split full name into parts (same pattern as donor registration)
        var nameParts = request.Name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (nameParts.Length < 3)
        {
            return Result<Guid>.Failure("Full name must include at least 3 names.");
        }

        // 3. Check for existing user
        var normalizedUserName = userManager.NormalizeName(request.NationalId);
        var existingUser = await userManager.FindByNameAsync(normalizedUserName);
        if (existingUser != null)
        {
            return Result<Guid>.Failure("A user with this National ID already exists.");
        }

        try
        {
            // 4. Create User entity
            var user = new User
            {
                UserName = request.NationalId,
                Email = request.Email,
                PhoneNumber = request.Phone,
                EmailConfirmed = true, // Trusted creation by Admin
                PhoneNumberConfirmed = true // Trusted creation by Admin
            };

            var createResult = await userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return Result<Guid>.Failure($"Failed to create user: {errors}");
            }

            // 5. Assign role
            var roleResult = await userManager.AddToRoleAsync(user, request.Role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return Result<Guid>.Failure($"Failed to assign role: {errors}");
            }

            // 6. Create Staff entity linked to the user
            var staff = new Staff
            {
                Id = user.Id, // FK to User.Id
                EmployeeIdentifier = $"EMP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                FirstName = nameParts[0],
                SecondName = nameParts[1],
                ThirdName = nameParts[2],
                FourthName = nameParts.Length > 3 ? nameParts[3] : null,
                PhoneNumber = request.Phone,
                Address = request.Address,
                City = request.City,
                DepartmentName = request.Role, // Department derived from the assigned role
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
