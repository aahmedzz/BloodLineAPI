using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.Auth;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Auth.Queries.GetCurrentStaffUser;

public sealed class GetCurrentStaffUserQueryHandler(
    UserManager<User> userManager,
    IApplicationDbContext dbContext)
    : IRequestHandler<GetCurrentStaffUserQuery, Result<AuthenticatedStaffUser>>
{
    public async Task<Result<AuthenticatedStaffUser>> Handle(GetCurrentStaffUserQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null || user.IsDeleted)
        {
            return Result<AuthenticatedStaffUser>.Failure("User not found.");
        }

        var staff = await dbContext.Staff
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == user.Id, cancellationToken);

        if (staff is null)
        {
            return Result<AuthenticatedStaffUser>.Failure("Staff profile not found.");
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

        return Result<AuthenticatedStaffUser>.Success(new AuthenticatedStaffUser(
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
        ));
    }
}
