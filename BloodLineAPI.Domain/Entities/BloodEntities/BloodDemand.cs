using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Domain.Entities.BloodEntities
{
    public class BloodDemand : AuditableEntity
    {
        public DateTime RequestDate { get; set; }
        public byte BloodTypeId { get; set; }
        public BloodType BloodType { get; set; } = null!;
        public string RequesterName { get; set; } = string.Empty;
        public int RequestedUnits { get; set; }
        public int IssuedUnits { get; set; } = 0;
        public BloodDemandPriority Priority { get; set; }
        public BloodDemandStatus Status { get; set; } = BloodDemandStatus.Pending;
        public string? Notes { get; set; }

        public int RemainingUnits => Math.Max(0, RequestedUnits - IssuedUnits);

        public ICollection<IssuanceRecord> IssuanceRecords { get; set; } = new List<IssuanceRecord>();
    }
}
