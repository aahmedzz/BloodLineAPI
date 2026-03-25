using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace BloodLineAPI.Application.Features.Auth.Commands.LoginMobileUser;

public sealed class LoginMobileUserCommandHandler(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IJwtGenerator jwtGenerator,
    ILogger<LoginMobileUserCommandHandler> logger) : IRequestHandler<LoginMobileUserCommand, Result<DonorAuthResponse>>
{
    public async Task<Result<DonorAuthResponse>> Handle(LoginMobileUserCommand request, CancellationToken cancellationToken)
    {
        var identifier = request.Identifier?.Trim();
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(password))
            return Result<DonorAuthResponse>.Failure("Identifier and password are required.");

        try
        {
            var user = await userManager.FindByNameAsync(identifier);
            if (user == null)
            {
                user = await userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == identifier, cancellationToken);
            }

            if (user == null || user.IsDeleted)
                return Result<DonorAuthResponse>.Failure("Invalid credentials.");

            var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

            if (!signInResult.Succeeded)
            {
                logger.LogWarning("Invalid login attempt for identifier {Identifier}", identifier);
                return Result<DonorAuthResponse>.Failure("Invalid credentials. If you forgot your password, use the forgot-password to reset it.");
            }

            var roles = await userManager.GetRolesAsync(user) ?? Enumerable.Empty<string>();
            var accessToken = jwtGenerator.GenerateToken(user, roles);
            var refreshToken = jwtGenerator.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to update user {UserId} refresh token: {Errors}", user.Id, errors);
                return Result<DonorAuthResponse>.Failure("An error occurred while updating user data.");
            }

            logger.LogInformation("User {UserId} logged in successfully.", user.Id);
            return Result<DonorAuthResponse>.Success(new DonorAuthResponse(accessToken, refreshToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while processing login for identifier {Identifier}", identifier);
            return Result<DonorAuthResponse>.Failure("An error occurred while processing the request.");
        }
    }
}
