using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Auth.Commands.RegisterMobileUser;

public sealed class RegisterMobileUserCommandHandler(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IApplicationDbContext dbContext,
    IRegistrationOtpService registrationOtpService)
    : IRequestHandler<RegisterMobileUserCommand, Result<RegisterMobileUserResponse>>
{
    public async Task<Result<RegisterMobileUserResponse>> Handle(RegisterMobileUserCommand request, CancellationToken cancellationToken)
    {
        var nameParts = request.FullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (nameParts.Length < 3)
        {
            return Result<RegisterMobileUserResponse>.Failure("Full name must include at least 3 names.");
        }

        var existingUser = await userManager.FindByNameAsync(request.NationalId);
        if (existingUser != null)
        {
            return Result<RegisterMobileUserResponse>.Failure("National ID is already registered.");
        }

        var phoneExists = await userManager.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);
        if (phoneExists)
        {
            return Result<RegisterMobileUserResponse>.Failure("Phone number is already registered.");
        }

        var user = new User
        {
            UserName = request.NationalId,
            PhoneNumber = request.PhoneNumber,
            PhoneNumberConfirmed = false
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<RegisterMobileUserResponse>.Failure(errors);
        }

        if (!await roleManager.RoleExistsAsync("Donor"))
        {
            await roleManager.CreateAsync(new Role { Name = "Donor" });
        }

        await userManager.AddToRoleAsync(user, "Donor");

        var donor = new Donor
        {
            Id = user.Id,
            FirstName = nameParts[0],
            SecondName = nameParts[1],
            ThirdName = nameParts[2],
            FourthName = nameParts.Length > 3 ? nameParts[3] : null,
            PhoneNumber = request.PhoneNumber,
            NationalId = request.NationalId
        };

        await dbContext.Donors.AddAsync(donor, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var otpResult = await registrationOtpService.GenerateStoreAndSendOTPAsync(user, cancellationToken);
        if (!otpResult.IsSuccess)
        {
            return Result<RegisterMobileUserResponse>.Failure($"Account created but failed to send verification code. {otpResult.Error}");
        }

        var userPayload = new AuthenticatedMobileUser(
            user.Id,
            request.NationalId,
            request.PhoneNumber,
            donor.FullName,
            user.PhoneNumberConfirmed,
            donor.IsRegistrationCompleted);

        return Result<RegisterMobileUserResponse>.Success(new RegisterMobileUserResponse(otpResult.Data!, true, userPayload));
    }
}
