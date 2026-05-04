namespace BloodLineAPI.Domain.Entities;

public class MedicalScreening : AuditableEntity
{
    public Guid DonorId { get; set; }
    public Guid PerformedByStaffId { get; set; }
    public DateTime ScreeningDate { get; set; }
    public decimal Weight { get; set; }
    public decimal SystolicBP { get; set; }
    public decimal DiastolicBP { get; set; }
    public decimal HemoglobinLevel { get; set; }
    public decimal Temperature { get; set; }
    public decimal PulseRate { get; set; }
    public bool IsEligible { get; set; }
    public bool HasChronicDiseases { get; set; }
    public string? ChronicDiseaseDetails { get; set; }
    public bool IsAllergic { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? LockoutUntil { get; set; }

    public Donor Donor { get; set; } = null!;
    public Staff PerformedByStaff { get; set; } = null!;
}

