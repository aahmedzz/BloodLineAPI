namespace BloodLineAPI.Domain.Entities
{
    public class InventoryTransaction : AuditableEntity
    {
        public Guid BloodBagId { get; set; }
        public Guid ExecutedByStaffId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string PreviousStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;

        public BloodBag BloodBag { get; set; } = null!;
        public Staff ExecutedByStaff { get; set; } = null!;
    }
}
