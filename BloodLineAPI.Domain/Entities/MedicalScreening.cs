namespace BloodBankSystem.Domain.Entities
{
    public class MedicalScreening :AuditableEntity
    {
        public Guid PerformedByStaffId { get; set; }
        public DateTime ScreeningDate { get; set; }
        public decimal Weight { get; set; }
        public decimal BloodPressure { get; set; }
        public decimal HemoglobinLevel { get; set; }
        public bool IsEligible { get; set; }
        public bool ChronicDiseases { get; set; }
        public bool IsAllergic { get; set; }
        public string? RejectionReason { get; set; }

        public Staff PerformedByStaff { get; set; } = null!;
    }
}
