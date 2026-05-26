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
    IOptions<DonationCooldownSettings> cooldownOptions)
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

        // 2. Lockout / Deferral check — query only the latest failed screening with active lockout
        var activeLockout = await dbContext.MedicalScreenings
            .Where(ms => ms.DonorId == donorId && !ms.IsEligible)
            .Where(ms => ms.LockoutUntil != null && ms.LockoutUntil > DateTime.UtcNow)
            .OrderByDescending(ms => ms.LockoutUntil)
            .Select(ms => new { ms.LockoutUntil, ms.RejectionReason })
            .FirstOrDefaultAsync(cancellationToken);

        if (activeLockout != null)
        {
            return Result<DonorEligibilityResult>.Success(
                new DonorEligibilityResult(
                    IsEligible: false,
                    DeferredUntil: activeLockout.LockoutUntil,
                    RejectionReason: $"Donor is deferred until {activeLockout.LockoutUntil:yyyy-MM-dd}: {activeLockout.RejectionReason}"));
        }

        // 3. Cooldown period check
        if (donor.LastDonationDate.HasValue)
        {
            var cooldownDays = cooldownOptions.Value.GetCooldownDays(donationType, donor.Gender);
            var daysSinceLast = (DateTime.UtcNow.Date - donor.LastDonationDate.Value.Date).TotalDays;

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
