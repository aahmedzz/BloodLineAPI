namespace BloodLineAPI.Domain.Entities.BloodEntities
{
    public class BloodBag : AuditableEntity
    {
        public string SerialNumber { get; set; } = string.Empty;
        public byte? BloodTypeId { get; set; }
        public Guid CollectedByStaffId { get; set; }
        public Guid? DonationAppointmentId { get; set; }
        public DateTime CollectionDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal Volume { get; set; }
        public BloodBagStatus Status { get; set; } = BloodBagStatus.Available;
        public DonationType BagType { get; set; } = DonationType.WholeBlood;

        public BloodType? BloodType { get; set; }
        public Staff CollectedByStaff { get; set; } = null!;
        public DonationAppointment? DonationAppointment { get; set; }
        public ICollection<BloodTestResult> BloodTestResults { get; set; } = new List<BloodTestResult>();
        public ICollection<BloodComponent> BloodComponents { get; set; } = new List<BloodComponent>();
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
        public DiscardRecord? DiscardRecord { get; set; }
        public IssuanceRecord? IssuanceRecord { get; set; }

        


    }
}  
