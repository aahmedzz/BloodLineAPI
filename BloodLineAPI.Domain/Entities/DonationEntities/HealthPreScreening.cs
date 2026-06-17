namespace BloodLineAPI.Domain.Entities.DonationEntities;

public class HealthPreScreening : AuditableEntity
{
    public Guid DonorId { get; private set; }
    public bool HasBeenThreeToFourMonthsSinceLastDonation { get; private set; }
    public bool HasAnyDisqualifyingCondition { get; private set; }
    public bool IsTakingBloodThinnersOrCriticalMedication { get; private set; }
    public bool HasRecentSurgeryInPast6Months { get; private set; }
    public bool HasRecentTattooOrPiercingInPast6Months { get; private set; }
    public bool HasDentalProcedureInPastWeek { get; private set; }
    public bool HasCurrentFeverInfectionOrSevereCold { get; private set; }
    public bool HasChronicIllnessAffectingBloodDonation { get; private set; }
    public bool IsEligible { get; private set; }
    public DateTime ScreenedAt { get; private set; }

    public Donor Donor { get; private set; } = null!;

    private HealthPreScreening()
    {
    }

    public static HealthPreScreening Create(
        Guid donorId,
        bool hasBeenThreeToFourMonthsSinceLastDonation,
        bool hasAnyDisqualifyingCondition,
        bool isTakingBloodThinnersOrCriticalMedication,
        bool hasRecentSurgeryInPast6Months,
        bool hasRecentTattooOrPiercingInPast6Months,
        bool hasDentalProcedureInPastWeek,
        bool hasCurrentFeverInfectionOrSevereCold,
        bool hasChronicIllnessAffectingBloodDonation,
        DateTime currentLocalTime)
    {
        var screening = new HealthPreScreening
        {
            Id = Guid.NewGuid(),
            DonorId = donorId,
            HasBeenThreeToFourMonthsSinceLastDonation = hasBeenThreeToFourMonthsSinceLastDonation,
            HasAnyDisqualifyingCondition = hasAnyDisqualifyingCondition,
            IsTakingBloodThinnersOrCriticalMedication = isTakingBloodThinnersOrCriticalMedication,
            HasRecentSurgeryInPast6Months = hasRecentSurgeryInPast6Months,
            HasRecentTattooOrPiercingInPast6Months = hasRecentTattooOrPiercingInPast6Months,
            HasDentalProcedureInPastWeek = hasDentalProcedureInPastWeek,
            HasCurrentFeverInfectionOrSevereCold = hasCurrentFeverInfectionOrSevereCold,
            HasChronicIllnessAffectingBloodDonation = hasChronicIllnessAffectingBloodDonation,
            ScreenedAt = currentLocalTime
        };

        screening.IsEligible = screening.EvaluateEligibility();
        return screening;
    }

    public bool IsStillValid() => IsEligible;

    private bool EvaluateEligibility()
    {
        return HasBeenThreeToFourMonthsSinceLastDonation
            && !HasAnyDisqualifyingCondition
            && !IsTakingBloodThinnersOrCriticalMedication
            && !HasRecentSurgeryInPast6Months
            && !HasRecentTattooOrPiercingInPast6Months
            && !HasDentalProcedureInPastWeek
            && !HasCurrentFeverInfectionOrSevereCold
            && !HasChronicIllnessAffectingBloodDonation;
    }
}
