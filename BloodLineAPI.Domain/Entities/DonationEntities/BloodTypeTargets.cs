using System;
using BloodLineAPI.Domain.Common;

namespace BloodLineAPI.Domain.Entities.DonationEntities
{
    public class BloodTypeTargets : BaseEntity
    {
        public Guid DonationCenterId { get; set; }
        public string BloodType { get; set; } = string.Empty;
        public int TargetCount { get; set; }

        // Navigation property
        public DonationCenter DonationCenter { get; set; } = null!;
    }
}
