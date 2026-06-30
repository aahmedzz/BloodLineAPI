using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Auth.Queries.GetCurrentMobileUser;

public sealed class GetCurrentMobileUserQueryHandler(
    UserManager<User> userManager,
    IApplicationDbContext dbContext,
    IDonorEligibilityService donorEligibilityService,
    IDateTimeProvider dateTimeProvider,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetCurrentMobileUserQuery, Result<MobileUserProfileResponse>>
{
    public async Task<Result<MobileUserProfileResponse>> Handle(GetCurrentMobileUserQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null || user.IsDeleted)
        {
            return Result<MobileUserProfileResponse>.Failure("User not found.");
        }

        var donor = await dbContext.Donors
            .Include(d => d.BloodType)
            .Include(d => d.DonorBadges)
                .ThenInclude(db => db.Badge)
            .FirstOrDefaultAsync(d => d.Id == user.Id, cancellationToken);

        if (donor == null)
        {
            return Result<MobileUserProfileResponse>.Failure("Donor profile not found.");
        }

        // Evaluate eligibility status using the shared service
        var eligibilityResult = await donorEligibilityService.CheckEligibilityAsync(
            donor.Id,
            DonationType.WholeBlood,
            cancellationToken);

        string status = "eligible";
        DateTime? deferredUntil = null;

        if (eligibilityResult.IsSuccess)
        {
            var eligibility = eligibilityResult.Data!;
            if (donor.Status == DonorStatus.Ineligible)
            {
                status = "ineligible";
            }
            else if (!eligibility.IsEligible)
            {
                status = "cooldown";
                deferredUntil = eligibility.DeferredUntil;
            }
        }

        // Build absolute base URL for icon paths
        var httpReq = httpContextAccessor.HttpContext?.Request;
        var baseUrl = httpReq is not null
            ? $"{httpReq.Scheme}://{httpReq.Host}{httpReq.PathBase}/"
            : string.Empty;

        // Retrieve last badge
        var lastDonorBadge = donor.DonorBadges
            .OrderByDescending(db => db.EarnedDate)
            .FirstOrDefault();

        var lastBadgeDto = lastDonorBadge != null ? new DonorBadgeDto(
            lastDonorBadge.Badge.BadgeKey,
            lastDonorBadge.Badge.BadgeName,
            lastDonorBadge.Badge.BadgeNameAr,
            lastDonorBadge.Badge.BadgeDescription,
            baseUrl + lastDonorBadge.Badge.IconUrl,
            lastDonorBadge.Badge.BadgeType.ToString(),
            lastDonorBadge.Badge.BonusPoints,
            lastDonorBadge.EarnedDate.ToString("yyyy-MM-dd")
        ) : null;

        var age = CalculateAge(donor.DateOfBirth, dateTimeProvider.UtcNow);

        var response = new MobileUserProfileResponse(
            UserId: user.Id,
            FullName: donor.FullName,
            BloodType: donor.BloodType?.FullDisplayname,
            TotalPoints: donor.TotalPoints,
            LastBadge: lastBadgeDto,
            NationalId: donor.NationalId,
            Gender: donor.Gender.ToString().ToLowerInvariant(),
            DateOfBirth: donor.DateOfBirth.ToString("yyyy-MM-dd"),
            Age: age,
            PhoneNumber: user.PhoneNumber ?? string.Empty,
            DonorCode: donor.DonorCode,
            WeightKg: donor.WeightKg,
            Governorate: donor.Governorate,
            District: donor.District,
            Area: donor.Area,
            Latitude: donor.Latitude,
            Longitude: donor.Longitude,
            TotalDonationCount: donor.TotalDonationCount,
            LastDonationDate: donor.LastDonationDate?.ToString("yyyy-MM-dd"),
            Status: status,
            DeferredUntil: deferredUntil?.ToString("yyyy-MM-dd"),
            IsPhoneNumberVerified: user.PhoneNumberConfirmed,
            IsRegistrationCompleted: donor.IsRegistrationCompleted
        );

        return Result<MobileUserProfileResponse>.Success(response);
    }

    private static int CalculateAge(DateOnly dateOfBirth, DateTime today)
    {
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > DateOnly.FromDateTime(today).AddYears(-age)) age--;
        return age;
    }
}
