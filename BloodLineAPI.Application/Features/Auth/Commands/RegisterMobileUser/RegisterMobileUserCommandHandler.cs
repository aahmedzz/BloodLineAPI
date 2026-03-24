using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace BloodLineAPI.Application.Features.Auth.Commands.RegisterMobileUser;

public sealed class RegisterMobileUserCommandHandler(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IApplicationDbContext dbContext,
    IJwtGenerator jwtGenerator) 
    : IRequestHandler<RegisterMobileUserCommand, Result<DonorAuthResponse>>
{
    public async Task<Result<DonorAuthResponse>> Handle(RegisterMobileUserCommand request, CancellationToken cancellationToken)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return Result<DonorAuthResponse>.Failure("Passwords do not match.");
        }

        var existingUser = await userManager.FindByNameAsync(request.NationalId);
        if (existingUser != null)
        {
            return Result<DonorAuthResponse>.Failure("National ID is already registered.");
        }

        var user = new User
        {
            UserName = request.NationalId,
            PhoneNumber = request.PhoneNumber
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<DonorAuthResponse>.Failure(errors);
        }

        if (!await roleManager.RoleExistsAsync("Donor"))
        {
            await roleManager.CreateAsync(new Role { Name = "Donor" });
        }

        await userManager.AddToRoleAsync(user, "Donor");

        var donor = new Donor
        {
            Id = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            NationalId = request.NationalId
        };

        var refreshToken = jwtGenerator.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await userManager.UpdateAsync(user);

        await dbContext.Donors.AddAsync(donor, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = jwtGenerator.GenerateToken(user, new[] { "Donor" });

        return Result<DonorAuthResponse>.Success(new DonorAuthResponse(token, refreshToken));
    }
}
