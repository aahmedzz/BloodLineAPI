using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donors.Queries.GetMyEligibility;

public sealed class GetMyEligibilityQueryHandler(
    IApplicationDbContext dbContext,
    IDonorEligibilityService donorEligibilityService,
    IDynamicSettingsService dynamicSettingsService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetMyEligibilityQuery, Result<MyEligibilityResponse>>
{
    public async Task<Result<MyEligibilityResponse>> Handle(GetMyEligibilityQuery request, CancellationToken cancellationToken)
    {
        var donor = await dbContext.Donors
            .Include(d => d.BloodType)
            .FirstOrDefaultAsync(d => d.Id == request.UserId, cancellationToken);

        if (donor == null)
        {
            return Result<MyEligibilityResponse>.Failure("Donor not found.");
        }

        var eligibilityResult = await donorEligibilityService.CheckEligibilityAsync(
            donor.Id,
            DonationType.WholeBlood,
            cancellationToken);

        if (!eligibilityResult.IsSuccess)
        {
            return Result<MyEligibilityResponse>.Failure(eligibilityResult.Error ?? "Failed to verify eligibility.");
        }

        var eligibility = eligibilityResult.Data!;
        var settings = await dynamicSettingsService.GetSettingsAsync(cancellationToken);
        var totalCooldownDays = settings.GetCooldownDays(DonationType.WholeBlood, donor.Gender);

        string status = "eligible";
        bool isEligible = true;
        DateOnly? nextEligibleDate = null;
        int? cooldownRemainingDays = null;
        double recoveryPercent = 100.0;
        DateTime? deferredUntil = null;
        string? deferralReason = null;

        if (donor.Status == DonorStatus.Ineligible)
        {
            status = "ineligible";
            isEligible = false;
            recoveryPercent = 0.0;
            deferralReason = eligibility.RejectionReason ?? "Donor is permanently ineligible.";
        }
        else if (!eligibility.IsEligible)
        {
            status = "cooldown";
            isEligible = false;

            if (eligibility.CooldownRemainingDays.HasValue)
            {
                cooldownRemainingDays = eligibility.CooldownRemainingDays.Value;
                nextEligibleDate = dateTimeProvider.CurrentLocalDate.AddDays(cooldownRemainingDays.Value);
                
                if (totalCooldownDays > 0)
                {
                    recoveryPercent = ((totalCooldownDays - cooldownRemainingDays.Value) / (double)totalCooldownDays) * 100.0;
                }
                else
                {
                    recoveryPercent = 0.0;
                }
            }
            else if (eligibility.DeferredUntil.HasValue)
            {
                deferredUntil = eligibility.DeferredUntil.Value;
                nextEligibleDate = DateOnly.FromDateTime(deferredUntil.Value);
                
                var remainingDays = (nextEligibleDate.Value.ToDateTime(TimeOnly.MinValue) - dateTimeProvider.CurrentLocalDate.ToDateTime(TimeOnly.MinValue)).Days;
                cooldownRemainingDays = Math.Max(1, remainingDays);
                
                var lockoutDuration = settings.DefaultScreeningLockoutDays > 0 ? settings.DefaultScreeningLockoutDays : 7;
                totalCooldownDays = lockoutDuration;
                
                recoveryPercent = ((lockoutDuration - cooldownRemainingDays.Value) / (double)lockoutDuration) * 100.0;
                deferralReason = eligibility.RejectionReason;
            }
            
            // Clamp progress percentage between 0 and 100
            recoveryPercent = Math.Max(0.0, Math.Min(100.0, recoveryPercent));
        }

        var response = new MyEligibilityResponse(
            Status: status,
            IsEligible: isEligible,
            LastDonationDate: donor.LastDonationDate?.ToString("yyyy-MM-dd"),
            NextEligibleDate: nextEligibleDate?.ToString("yyyy-MM-dd"),
            CooldownRemainingDays: cooldownRemainingDays,
            TotalCooldownDays: totalCooldownDays,
            RecoveryPercent: Math.Round(recoveryPercent, 1),
            DeferredUntil: deferredUntil?.ToString("yyyy-MM-dd"),
            DeferralReason: deferralReason
        );

        return Result<MyEligibilityResponse>.Success(response);
    }
}
