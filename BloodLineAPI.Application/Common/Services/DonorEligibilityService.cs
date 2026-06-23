using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Common;
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
}
