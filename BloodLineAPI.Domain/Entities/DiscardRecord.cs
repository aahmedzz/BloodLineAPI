

namespace BloodBankSystem.Domain.Entities
{
    public class DiscardRecord : AuditableEntity
    {
        public Guid BloodBagId { get; set; }
        public Guid AuthorizedByStaffId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime DiscardDate { get; set; }

        public BloodBag BloodBag { get; set; } = null!;
        public Staff AuthorizedByStaff { get; set; } = null!;
    }
}
