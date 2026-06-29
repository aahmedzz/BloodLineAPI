using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BloodLineAPI.Application.Common.Services;

/// <summary>
/// Centralised donor eligibility checker.
/// Validates lockout/deferral, cooldown period, and donor status
/// so that every flow (system, mobile, campaign) shares the same rules.
/// </summary>
public sealed class DonorEligibilityService(
    IApplicationDbContext dbContext,
    IDynamicSettingsService dynamicSettingsService,
    IDateTimeProvider dateTimeProvider)
    : IDonorEligibilityService
{
    public async Task<Result<DonorEligibilityResult>> CheckEligibilityAsync(
        Guid donorId,
        DonationType donationType,
        CancellationToken cancellationToken = default)
    {
        var donor = await dbContext.Donors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == donorId, cancellationToken);

        if (donor == null)
        {
            return Result<DonorEligibilityResult>.Failure("Donor not found.");
        }

        // 1. Donor Status check
        if (donor.Status == DonorStatus.Ineligible)
        {
            return Result<DonorEligibilityResult>.Success(
                new DonorEligibilityResult(
                    IsEligible: false,
                    RejectionReason: "Donor is permanently ineligible."));
        }

        // 2. Lockout / Deferral check
        if (donor.Status == DonorStatus.Deferred && donor.LockoutUntil.HasValue && donor.LockoutUntil.Value > dateTimeProvider.UtcNow)
        {
            var localLockout = dateTimeProvider.ToLocalTime(donor.LockoutUntil.Value);
            return Result<DonorEligibilityResult>.Success(
                new DonorEligibilityResult(
                    IsEligible: false,
                    DeferredUntil: donor.LockoutUntil,
                    RejectionReason: $"Donor is deferred until {localLockout:yyyy-MM-dd}"));
        }

        // 3. Cooldown period check
        if (donor.LastDonationDate.HasValue)
        {
            var settings = await dynamicSettingsService.GetSettingsAsync(cancellationToken);
            var cooldownDays = settings.GetCooldownDays(donationType, donor.Gender);
            var daysSinceLast = (dateTimeProvider.CurrentLocalDate.ToDateTime(TimeOnly.MinValue) - donor.LastDonationDate.Value.Date).TotalDays;

            if (daysSinceLast < cooldownDays)
            {
                var remaining = (int)Math.Ceiling(cooldownDays - daysSinceLast);
                return Result<DonorEligibilityResult>.Success(
                    new DonorEligibilityResult(
                        IsEligible: false,
                        CooldownRemainingDays: remaining,
                        RejectionReason: $"Must wait {remaining} more day(s) before donating again. Cooldown: {cooldownDays} days."));
            }
        }

        // All checks passed
        return Result<DonorEligibilityResult>.Success(
            new DonorEligibilityResult(IsEligible: true));
    }

    public async Task<IQueryable<Donor>> FilterDonorsAsync(
        IQueryable<Donor> query,
        DonorEligibilityFiltersDto filters,
        CancellationToken cancellationToken = default)
    {
        var settings = await dynamicSettingsService.GetSettingsAsync(cancellationToken);
        var maleDays = settings.WholeBloodMaleDays;
        var femaleDays = settings.WholeBloodFemaleDays;
        var todayLocal = dateTimeProvider.LocalNow.Date;

        // 1. Search Filter (Name, Phone, NationalId, DonorCode)
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(d =>
                d.FirstName.Contains(search) ||
                d.SecondName.Contains(search) ||
                d.ThirdName.Contains(search) ||
                (d.FourthName != null && d.FourthName.Contains(search)) ||
                (d.FirstName + " " + d.SecondName + " " + d.ThirdName + " " + (d.FourthName ?? "")).Contains(search) ||
                d.PhoneNumber.Contains(search) ||
                d.NationalId.Contains(search) ||
                d.DonorCode.Contains(search));
        }

        // 2. Blood Type Filter (e.g., "A+", "O-")
        if (!string.IsNullOrWhiteSpace(filters.BloodType))
        {
            var bloodTypeStr = filters.BloodType.Trim().ToUpperInvariant();
            var hasSign = bloodTypeStr.EndsWith('+') || bloodTypeStr.EndsWith('-');
            if (hasSign)
            {
                var groupStr = bloodTypeStr[..^1];
                var sign = bloodTypeStr[^1];
                if (Enum.TryParse<BloodGroupName>(groupStr, true, out var groupName))
                {
                    var rhFactor = sign == '+' ? RhFactor.Positive : RhFactor.Negative;
                    query = query.Where(d => d.BloodType != null && d.BloodType.BloodGroupName == groupName && d.BloodType.RhFactor == rhFactor);
                }
            }
        }

        // 3. District Filter
        if (!string.IsNullOrWhiteSpace(filters.District))
        {
            var dist = filters.District.Trim();
            query = query.Where(d => d.District != null && d.District.Contains(dist));
        }

        // 4. Gender Filter
        if (!string.IsNullOrWhiteSpace(filters.Gender))
        {
            if (Enum.TryParse<Gender>(filters.Gender, true, out var genderEnum))
            {
                query = query.Where(d => d.Gender == genderEnum);
            }
        }

        // 5. Status Filter
        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            var statusLower = filters.Status.Trim().ToLowerInvariant();
            switch (statusLower)
            {
                case "ineligible":
                    query = query.Where(d => d.Status == DonorStatus.Ineligible);
                    break;

                case "deferred":
                    query = query.Where(d => d.Status == DonorStatus.Deferred);
                    break;

                case "eligible":
                    query = query.Where(d => d.Status == DonorStatus.Eligible &&
                        (d.LastDonationDate == null ||
                         d.LastDonationDate.Value.AddDays(d.Gender == Gender.Male ? maleDays : femaleDays) <= todayLocal));
                    break;

                case "soon":
                    query = query.Where(d => d.Status == DonorStatus.Eligible &&
                        d.LastDonationDate != null &&
                        d.LastDonationDate.Value.AddDays(d.Gender == Gender.Male ? maleDays : femaleDays) > todayLocal &&
                        d.LastDonationDate.Value.AddDays(d.Gender == Gender.Male ? maleDays - 14 : femaleDays - 14) <= todayLocal);
                    break;

                case "not_yet":
                    query = query.Where(d => d.Status == DonorStatus.Eligible &&
                        d.LastDonationDate != null &&
                        d.LastDonationDate.Value.AddDays(d.Gender == Gender.Male ? maleDays - 14 : femaleDays - 14) > todayLocal);
                    break;
            }
        }

        // 6. Mobile App Filter
        if (filters.HasMobileApp.HasValue)
        {
            if (filters.HasMobileApp.Value)
            {
                query = query.Where(d => d.User != null && d.User.PasswordHash != null);
            }
            else
            {
                query = query.Where(d => d.User == null || d.User.PasswordHash == null);
            }
        }

        return query;
    }
}
