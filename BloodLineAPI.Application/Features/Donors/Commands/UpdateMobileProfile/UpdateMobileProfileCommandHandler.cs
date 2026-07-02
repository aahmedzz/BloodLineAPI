using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.Users;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donors.Commands.UpdateMobileProfile;

public sealed class UpdateMobileProfileCommandHandler(
    UserManager<User> userManager,
    IApplicationDbContext dbContext,
    IDonorEligibilityService donorEligibilityService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<UpdateMobileProfileCommand, Result<MobileUserProfileResponse>>
{
    public async Task<Result<MobileUserProfileResponse>> Handle(UpdateMobileProfileCommand request, CancellationToken cancellationToken)
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

        // 1. Update DateOfBirth and validate age >= 18
        if (!string.IsNullOrWhiteSpace(request.DateOfBirth))
        {
            if (DateOnly.TryParse(request.DateOfBirth, out var parsedBirthDate))
            {
                var today = dateTimeProvider.UtcNow;
                var calculatedAge = today.Year - parsedBirthDate.Year;
                if (parsedBirthDate > DateOnly.FromDateTime(today).AddYears(-calculatedAge)) calculatedAge--;
                if (calculatedAge < 18)
                {
                    return Result<MobileUserProfileResponse>.Failure("Donor must be at least 18 years old.");
                }
                donor.DateOfBirth = parsedBirthDate;
            }
            else
            {
                return Result<MobileUserProfileResponse>.Failure("Invalid date format. Use yyyy-MM-dd.");
            }
        }

        // 2. Update Phone
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var cleanPhone = request.PhoneNumber.Trim();
            if (cleanPhone != donor.PhoneNumber)
            {
                var phoneExists = await dbContext.Donors.AnyAsync(d => d.Id != donor.Id && d.PhoneNumber == cleanPhone, cancellationToken);
                if (phoneExists)
                {
                    return Result<MobileUserProfileResponse>.Failure("Phone number is already registered to another account.");
                }
                donor.PhoneNumber = cleanPhone;
                user.PhoneNumber = cleanPhone;
            }
        }

        // 3. Update Weight
        if (request.WeightKg.HasValue)
        {
            if (request.WeightKg.Value < 40 || request.WeightKg.Value > 200)
            {
                return Result<MobileUserProfileResponse>.Failure("Weight must be between 40 and 200 kg.");
            }
            donor.WeightKg = request.WeightKg.Value;
        }

        // 4. Update address components and rebuild Address property
        bool addressChanged = false;
        if (request.Governorate != null)
        {
            donor.Governorate = string.IsNullOrWhiteSpace(request.Governorate) ? null : request.Governorate.Trim();
            addressChanged = true;
        }
        if (request.District != null)
        {
            donor.District = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim();
            addressChanged = true;
        }
        if (request.Area != null)
        {
            donor.Area = string.IsNullOrWhiteSpace(request.Area) ? null : request.Area.Trim();
            addressChanged = true;
        }

        if (addressChanged)
        {
            var addressParts = new[] { donor.Governorate, donor.District, donor.Area }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            donor.Address = string.Join(", ", addressParts);
        }

        // Save changes to DB
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result<MobileUserProfileResponse>.Failure(errors);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        // Fetch updated donor and build the response
        var updatedDonor = await dbContext.Donors
            .Include(d => d.BloodType)
            .Include(d => d.DonorBadges)
                .ThenInclude(db => db.Badge)
            .FirstOrDefaultAsync(d => d.Id == user.Id, cancellationToken);

        if (updatedDonor == null)
        {
            return Result<MobileUserProfileResponse>.Failure("Donor profile not found after update.");
        }

        var eligibilityResult = await donorEligibilityService.CheckEligibilityAsync(
            updatedDonor.Id,
            DonationType.WholeBlood,
            cancellationToken);

        string status = "eligible";
        DateTime? deferredUntil = null;

        if (eligibilityResult.IsSuccess)
        {
            var eligibility = eligibilityResult.Data!;
            if (updatedDonor.Status == DonorStatus.Ineligible)
            {
                status = "ineligible";
            }
            else if (!eligibility.IsEligible)
            {
                status = "cooldown";
                deferredUntil = eligibility.DeferredUntil;
            }
        }

        var lastDonorBadge = updatedDonor.DonorBadges
            .OrderByDescending(db => db.EarnedDate)
            .FirstOrDefault();

        var lastBadgeDto = lastDonorBadge != null ? new DonorBadgeDto(
            lastDonorBadge.Badge.BadgeKey,
            lastDonorBadge.Badge.BadgeName,
            lastDonorBadge.Badge.BadgeNameAr,
            lastDonorBadge.Badge.BadgeDescription,
            lastDonorBadge.Badge.BadgeDescriptionAr,
            lastDonorBadge.Badge.IconUrl,
            lastDonorBadge.Badge.BadgeType.ToString(),
            lastDonorBadge.Badge.BonusPoints,
            lastDonorBadge.EarnedDate.ToString("yyyy-MM-dd")
        ) : null;

        var age = CalculateAge(updatedDonor.DateOfBirth, dateTimeProvider.UtcNow);

        var response = new MobileUserProfileResponse(
            UserId: user.Id,
            FullName: updatedDonor.FullName,
            BloodType: updatedDonor.BloodType?.FullDisplayname,
            TotalPoints: updatedDonor.TotalPoints,
            LastBadge: lastBadgeDto,
            NationalId: updatedDonor.NationalId,
            Gender: updatedDonor.Gender.ToString().ToLowerInvariant(),
            DateOfBirth: updatedDonor.DateOfBirth.ToString("yyyy-MM-dd"),
            Age: age,
            PhoneNumber: user.PhoneNumber ?? string.Empty,
            DonorCode: updatedDonor.DonorCode,
            WeightKg: updatedDonor.WeightKg,
            Governorate: updatedDonor.Governorate,
            District: updatedDonor.District,
            Area: updatedDonor.Area,
            TotalDonationCount: updatedDonor.TotalDonationCount,
            LastDonationDate: updatedDonor.LastDonationDate?.ToString("yyyy-MM-dd"),
            Status: status,
            DeferredUntil: deferredUntil?.ToString("yyyy-MM-dd"),
            IsPhoneNumberVerified: user.PhoneNumberConfirmed,
            IsRegistrationCompleted: updatedDonor.IsRegistrationCompleted
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
