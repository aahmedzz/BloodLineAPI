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
        var identifier = request.NationalId?.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
            return Result<StaffAuthResponse>.Failure("National ID and password are required.");

        try
        {
            // For staff, we only allow login by NationalId (which is UserName)
            var normalizedIdentifier = userManager.NormalizeName(identifier);
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedIdentifier, cancellationToken);

            if (user == null || user.IsDeleted)
                return Result<StaffAuthResponse>.Failure("Invalid credentials.");

            var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

            if (!signInResult.Succeeded)
            {
                logger.LogWarning("Invalid login attempt for staff NationalId {NationalId}", identifier);
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

            // For simplicity, just pick the first role. In a real app you might want to handle multiple roles.
            var primaryRole = roles.FirstOrDefault() ?? string.Empty;

            var userPayload = new AuthenticatedStaffUser(
                UserId: user.Id,
                NationalId: user.UserName ?? string.Empty,
                FullName: staff.FullName ?? string.Empty,
                Role: primaryRole,
                DepartmentName: staff.DepartmentName ?? string.Empty,
                IsActiveEmployee: staff.IsActiveEmployee
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
            logger.LogError(ex, "Error while processing staff login for NationalId {NationalId}", identifier);
            return Result<StaffAuthResponse>.Failure("An error occurred while processing the request.");
        }
    }
}
