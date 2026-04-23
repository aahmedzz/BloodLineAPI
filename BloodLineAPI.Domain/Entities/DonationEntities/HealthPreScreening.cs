namespace BloodLineAPI.Domain.Entities.DonationEntities;

public class HealthPreScreening : AuditableEntity
{
    public Guid DonorId { get; private set; }
    public bool HasChronicDisease { get; private set; }
    public bool HasRecentSurgery { get; private set; }
    public bool IsTakingMedication { get; private set; }
    public bool HasRecentTattooOrPiercing { get; private set; }
    public bool HasRecentInfection { get; private set; }
    public bool IsPregnantOrBreastfeeding { get; private set; }
    public bool HasBleedingDisorder { get; private set; }
    public bool HasRecentVaccination { get; private set; }
    public bool IsEligible { get; private set; }
    public DateTime ScreenedAt { get; private set; }

    public Donor Donor { get; private set; } = null!;

    private HealthPreScreening()
    {
    }

    public static HealthPreScreening Create(
        Guid donorId,
        bool hasChronicDisease,
        bool hasRecentSurgery,
        bool isTakingMedication,
        bool hasRecentTattooOrPiercing,
        bool hasRecentInfection,
        bool isPregnantOrBreastfeeding,
        bool hasBleedingDisorder,
        bool hasRecentVaccination)
    {
        var now = DateTime.UtcNow;
        var screening = new HealthPreScreening
        {
            Id = Guid.NewGuid(),
            DonorId = donorId,
            HasChronicDisease = hasChronicDisease,
            HasRecentSurgery = hasRecentSurgery,
            IsTakingMedication = isTakingMedication,
            HasRecentTattooOrPiercing = hasRecentTattooOrPiercing,
            HasRecentInfection = hasRecentInfection,
            IsPregnantOrBreastfeeding = isPregnantOrBreastfeeding,
            HasBleedingDisorder = hasBleedingDisorder,
            HasRecentVaccination = hasRecentVaccination,
            ScreenedAt = now
        };

        screening.IsEligible = screening.EvaluateEligibility();
        return screening;
    }

    public bool IsStillValid() => IsEligible;

    private bool EvaluateEligibility()
    {
        return !HasChronicDisease
            && !HasRecentSurgery
            && !IsTakingMedication
            && !HasRecentTattooOrPiercing
            && !HasRecentInfection
            && !IsPregnantOrBreastfeeding
            && !HasBleedingDisorder
            && !HasRecentVaccination;
    }
}
