using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.Auth;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Application.Features.Auth.Commands.LoginStaffUser;

public sealed class LoginStaffUserCommandHandler(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IJwtGenerator jwtGenerator,
    IApplicationDbContext dbContext,
    ILogger<LoginStaffUserCommandHandler> logger) : IRequestHandler<LoginStaffUserCommand, Result<StaffAuthResponse>>
{
    public async Task<Result<StaffAuthResponse>> Handle(LoginStaffUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Result<StaffAuthResponse>.Failure("Email and password are required.");

        try
        {
            // Find user by email
            var user = await userManager.FindByEmailAsync(email);

            if (user == null || user.IsDeleted)
                return Result<StaffAuthResponse>.Failure("Invalid credentials.");

            var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

            if (!signInResult.Succeeded)
            {
                logger.LogWarning("Invalid login attempt for staff email {Email}", email);
                return Result<StaffAuthResponse>.Failure("Invalid credentials.");
            }

            // Verify this user has a Staff record
            var staff = await dbContext.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == user.Id, cancellationToken);

            if (staff == null)
            {
                logger.LogWarning("Login attempt by user {UserId} who does not have a Staff profile", user.Id);
                return Result<StaffAuthResponse>.Failure("User is not authorized as staff.");
            }

            if (!staff.IsActiveEmployee)
            {
                logger.LogWarning("Login attempt by inactive staff {UserId}", user.Id);
                return Result<StaffAuthResponse>.Failure("This staff account is deactivated.");
            }

            var roles = await userManager.GetRolesAsync(user) ?? new List<string>();
            var isStaffRole = roles.Any(r => r is "Admin" or "Doctor" or "LabDoctor" or "InventoryManager");
            
            if (!isStaffRole)
            {
                logger.LogWarning("Login attempt by user {UserId} who does not have any staff roles", user.Id);
                return Result<StaffAuthResponse>.Failure("User does not have the necessary permissions.");
            }

            // Map role to frontend format
            var primaryRole = roles.FirstOrDefault() ?? string.Empty;
            var mappedRole = primaryRole switch
            {
                "Admin" => "admin",
                "Doctor" => "doctor",
                "LabDoctor" => "lab",
                "InventoryManager" => "inventory",
                _ => primaryRole.ToLowerInvariant()
            };

            var userPayload = new AuthenticatedStaffUser(
                Id: user.Id,
                Name: staff.FullName ?? string.Empty,
                Email: user.Email ?? string.Empty,
                Role: mappedRole,
                NationalId: user.UserName ?? string.Empty,
                Phone: staff.PhoneNumber ?? string.Empty,
                Address: staff.Address ?? string.Empty,
                City: staff.City ?? string.Empty,
                Status: staff.IsActiveEmployee ? "active" : "inactive",
                CreatedAt: staff.CreatedAt
            );

            var refreshToken = jwtGenerator.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to update staff {UserId} refresh token: {Errors}", user.Id, errors);
                return Result<StaffAuthResponse>.Failure("An error occurred while updating user data.");
            }

            var accessToken = jwtGenerator.GenerateToken(user, roles);

            logger.LogInformation("Staff {UserId} logged in successfully.", user.Id);
            return Result<StaffAuthResponse>.Success(new StaffAuthResponse(accessToken, refreshToken, userPayload));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while processing staff login for email {Email}", email);
            return Result<StaffAuthResponse>.Failure("An error occurred while processing the request.");
        }
    }
}
