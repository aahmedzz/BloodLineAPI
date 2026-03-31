using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BloodLineAPI.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    UserManager<User> userManager,
    IApplicationDbContext dbContext,
    IJwtGenerator jwtGenerator) 
    : IRequestHandler<RefreshTokenCommand, Result<DonorAuthResponse>>
{
    public async Task<Result<DonorAuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = jwtGenerator.GetPrincipalFromExpiredToken(request.Token);
        if (principal == null)
            return Result<DonorAuthResponse>.Failure("Invalid access token or refresh token");

        var userIdString = principal.FindFirst("sub")?.Value ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null)
            return Result<DonorAuthResponse>.Failure("Invalid access token or refresh token");

        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Result<DonorAuthResponse>.Failure("Invalid access token or refresh token");
        }

        var roles = await userManager.GetRolesAsync(user);
        var newAccessToken = jwtGenerator.GenerateToken(user, roles);
        var newRefreshToken = jwtGenerator.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await userManager.UpdateAsync(user);

        var donor = await dbContext.Donors.FirstOrDefaultAsync(d => d.Id == user.Id, cancellationToken);
        var userPayload = new AuthenticatedMobileUser(
            user.Id,
            user.UserName ?? string.Empty,
            user.PhoneNumber ?? string.Empty,
            donor?.FullName ?? string.Empty,
            user.PhoneNumberConfirmed,
            donor?.IsRegistrationCompleted ?? false);

        return Result<DonorAuthResponse>.Success(new DonorAuthResponse(newAccessToken, newRefreshToken, userPayload));
    }
}
