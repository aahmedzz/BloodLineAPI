namespace BloodBankSystem.Domain.Entities.BloodEntities
{
    public class BloodBag : AuditableEntity
    {
        public string SerialNumber { get; set; } = string.Empty;
        public Guid BloodTypeEntityId { get; set; }
        public Guid CollectedByStaffId { get; set; }
        public Guid? DonationAppointmentId { get; set; }
        public DateTime CollectionDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal Volume { get; set; }
        public BloodBagStatus Status { get; set; } = BloodBagStatus.Available;

        public BloodType BloodTypeEntity { get; set; } = null!;
        public Staff CollectedByStaff { get; set; } = null!;
        public DonationAppointment? DonationAppointment { get; set; }
        public ICollection<BloodTestResult> BloodTestResults { get; set; } = new List<BloodTestResult>();
        public ICollection<BloodComponent> BloodComponents { get; set; } = new List<BloodComponent>();
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
        public DiscardRecord? DiscardRecord { get; set; }
    }
}
