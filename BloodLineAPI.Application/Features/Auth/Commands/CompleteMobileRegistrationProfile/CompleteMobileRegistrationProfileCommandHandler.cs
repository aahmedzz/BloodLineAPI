using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Auth.Commands.CompleteMobileRegistrationProfile;

public sealed class CompleteMobileRegistrationProfileCommandHandler(
    IApplicationDbContext dbContext,
    UserManager<User> userManager,
    IJwtGenerator jwtGenerator)
    : IRequestHandler<CompleteMobileRegistrationProfileCommand, Result<DonorAuthResponse>>
{
    public async Task<Result<DonorAuthResponse>> Handle(CompleteMobileRegistrationProfileCommand request, CancellationToken cancellationToken)
    {
        var donor = await dbContext.Donors.FirstOrDefaultAsync(d => d.Id == request.UserId, cancellationToken);
        if (donor is null)
        {
            return Result<DonorAuthResponse>.Failure("Donor account was not found.");
        }

        var bloodType = await dbContext.BloodTypes
            .FirstOrDefaultAsync(bt => bt.BloodGroupName == request.BloodGroupName && bt.RhFactor == request.RhFactor, cancellationToken);

        if (bloodType is null)
        {
            return Result<DonorAuthResponse>.Failure("Selected blood type is not available.");
        }

        var governorate = request.Governorate.Trim();
        var district = request.District.Trim();
        var area = request.Area.Trim();

        donor.DateOfBirth = request.DateOfBirth;
        donor.Gender = request.Gender;
        donor.BloodTypeId = bloodType.Id;
        donor.Governorate = governorate;
        donor.District = district;
        donor.Area = area;
        donor.Address = string.Join(", ", new[] { area, district, governorate }.Where(x => !string.IsNullOrWhiteSpace(x)));
        donor.WeightKg = request.WeightKg;
        donor.IsRegistrationCompleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result<DonorAuthResponse>.Failure("User account was not found.");
        }

        var refreshToken = jwtGenerator.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = jwtGenerator.GenerateToken(user, roles);

        var userPayload = new AuthenticatedMobileUser(
            user.Id,
            user.UserName ?? string.Empty,
            user.PhoneNumber ?? string.Empty,
            donor.FullName,
            user.PhoneNumberConfirmed,
            donor.IsRegistrationCompleted);

        return Result<DonorAuthResponse>.Success(new DonorAuthResponse(accessToken, refreshToken, userPayload));
    }
}
