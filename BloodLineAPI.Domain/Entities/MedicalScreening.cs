namespace BloodLineAPI.Domain.Entities;

public class MedicalScreening : AuditableEntity
{
    public Guid DonorId { get; set; }
    public Guid PerformedByStaffId { get; set; }
    public DateTime ScreeningDate { get; set; }
    public decimal Weight { get; set; }
    public decimal BloodPressure { get; set; }
    public decimal HemoglobinLevel { get; set; }
    public bool IsEligible { get; set; }
    public bool ChronicDiseases { get; set; }
    public bool IsAllergic { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? LockoutUntil { get; set; }

    public Donor Donor { get; set; } = null!;
    public Staff PerformedByStaff { get; set; } = null!;
}

