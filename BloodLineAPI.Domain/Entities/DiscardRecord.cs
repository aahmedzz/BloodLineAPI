

namespace BloodLineAPI.Domain.Entities
{
    public class DiscardRecord : AuditableEntity
    {
        public Guid BloodBagId { get; set; }
        public Guid AuthorizedByStaffId { get; set; }
        public DiscardReason ReasonCategory { get; set; }
        public string? ReasonDetails { get; set; }
        public DateTime DiscardDate { get; set; }

        public BloodBag BloodBag { get; set; } = null!;
        public Staff AuthorizedByStaff { get; set; } = null!;
    }
}
