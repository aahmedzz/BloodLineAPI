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
    : IRequestHandler<RegisterMobileUserCommand, Result<string>>
{
    public async Task<Result<string>> Handle(RegisterMobileUserCommand request, CancellationToken cancellationToken)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return Result<string>.Failure("Passwords do not match.");
        }

        var existingUser = await userManager.FindByNameAsync(request.NationalId);
        if (existingUser != null)
        {
            return Result<string>.Failure("National ID is already registered.");
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
            return Result<string>.Failure(errors);
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

        await dbContext.Donors.AddAsync(donor, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = jwtGenerator.GenerateToken(user, new[] { "Donor" });

        return Result<string>.Success(token);
    }
}
