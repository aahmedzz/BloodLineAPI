using BloodLineAPI.Domain.Entities.BloodEntities;

namespace BloodLineAPI.Domain.Entities
{
    public class IssuanceRecord : AuditableEntity
    {
        public Guid BloodBagId { get; set; }
        public Guid IssuedByStaffId { get; set; }
        public DateTime IssuedAt { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid? BloodDemandId { get; set; }

        public BloodBag BloodBag { get; set; } = null!;
        public Staff IssuedByStaff { get; set; } = null!;
        public BloodDemand? BloodDemand { get; set; }
    }
}
