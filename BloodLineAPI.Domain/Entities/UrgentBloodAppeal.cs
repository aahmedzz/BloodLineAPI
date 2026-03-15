namespace BloodBankSystem.Domain.Entities
{
    public class UrgentBloodAppeal : AuditableEntity
    {
        public Guid CreatedByStaffId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TargetDistrict { get; set; } = string.Empty;
        public int TargetBagsNeeded { get; set; }
        public int CurrentBagsCollected { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime BroadcastDate { get; set; }

        public Staff CreatedByStaff { get; set; } = null!;
        public ICollection<BloodTypeEntity> TargetedBloodTypes { get; set; } = new List<BloodTypeEntity>();

    }
}
