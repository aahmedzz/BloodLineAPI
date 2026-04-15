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
}
