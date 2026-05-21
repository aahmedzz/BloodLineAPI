using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.Auth;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BloodLineAPI.Application.Features.Auth.Commands.RefreshStaffToken;

public sealed class RefreshStaffTokenCommandHandler(
    UserManager<User> userManager,
    IApplicationDbContext dbContext,
    IJwtGenerator jwtGenerator) 
    : IRequestHandler<RefreshStaffTokenCommand, Result<StaffAuthResponse>>
{
    public async Task<Result<StaffAuthResponse>> Handle(RefreshStaffTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = jwtGenerator.GetPrincipalFromExpiredToken(request.Token);
        if (principal == null)
            return Result<StaffAuthResponse>.Failure("Invalid access token or refresh token");

        var userIdString = principal.FindFirst("sub")?.Value ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdString == null)
            return Result<StaffAuthResponse>.Failure("Invalid access token or refresh token");

        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Result<StaffAuthResponse>.Failure("Invalid access token or refresh token");
        }

        var staff = await dbContext.Staff.FirstOrDefaultAsync(s => s.Id == user.Id, cancellationToken);
        if (staff == null || !staff.IsActiveEmployee)
        {
             return Result<StaffAuthResponse>.Failure("User is not authorized as staff or account is inactive.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? string.Empty;
        var mappedRole = primaryRole switch
        {
            "Admin" => "admin",
            "Doctor" => "doctor",
            "LabDoctor" => "lab",
            "InventoryManager" => "inventory",
            _ => primaryRole.ToLowerInvariant()
        };

        var newAccessToken = jwtGenerator.GenerateToken(user, roles);
        var newRefreshToken = jwtGenerator.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await userManager.UpdateAsync(user);

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

        return Result<StaffAuthResponse>.Success(new StaffAuthResponse(newAccessToken, newRefreshToken, userPayload));
    }
}
