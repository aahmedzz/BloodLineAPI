namespace BloodLineAPI.Domain.Common;

public class DonationCooldownSettings
{
    public int WholeBloodMaleDays { get; set; } = 90;
    public int WholeBloodFemaleDays { get; set; } = 120;
    public int PlasmaDays { get; set; } = 28;
    public int PlateletsDays { get; set; } = 7;

    public int GetCooldownDays(DonationType type, Gender gender) => type switch
    {
        DonationType.WholeBlood => gender == Gender.Female ? WholeBloodFemaleDays : WholeBloodMaleDays,
        DonationType.Plasma => PlasmaDays,
        DonationType.Platelets => PlateletsDays,
        _ => WholeBloodMaleDays
    };
}

