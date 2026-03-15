namespace BloodBankSystem.Domain.Entities.BloodEntities
{
    public class BloodTestResult : AuditableEntity
    {
        public Guid BloodBagId { get; set; }
        public Guid TestedByStaffId { get; set; }
        public DateTime TestDate { get; set; }
        public bool IsSafe { get; set; }
        public string? TestFileUrl { get; set; }
        public string? HepatitisResult { get; set; }
        public string? HepatitisBResult { get; set; }
        public string? HivResult { get; set; }

        public BloodBag BloodBag { get; set; } = null!;
        public Staff TestedByStaff { get; set; } = null!;
    }
}
