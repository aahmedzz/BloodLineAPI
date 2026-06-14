using System;
using System.Linq;
using System.Linq.Expressions;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Domain.Extensions;

public static class DonorExtensions
{
    public static bool IsEligibleForRegularDonationAt(this Donor donor, DateTime occurredOn)
    {
        if (!donor.LastDonationDate.HasValue)
        {
            return true;
        }

        var minMonths = donor.Gender == Gender.Female ? 4 : 3;
        var nextEligibleDate = donor.LastDonationDate.Value.AddMonths(minMonths);

        return occurredOn >= nextEligibleDate;
    }

    public static Expression<Func<Donor, bool>> IsEligiblePredicate(
        DateTime todayDate, 
        DateTime utcNow, 
        DonationCooldownSettings cooldownSettings)
    {
        return d => d.Status != DonorStatus.Ineligible && 
                    (!d.LastDonationDate.HasValue || 
                     (d.Gender == Gender.Male 
                         ? d.LastDonationDate.Value.AddDays(cooldownSettings.WholeBloodMaleDays) <= todayDate 
                         : d.LastDonationDate.Value.AddDays(cooldownSettings.WholeBloodFemaleDays) <= todayDate)) &&
                    !d.MedicalScreenings.Any(ms => !ms.IsEligible && ms.LockoutUntil != null && ms.LockoutUntil > utcNow);
    }
}
