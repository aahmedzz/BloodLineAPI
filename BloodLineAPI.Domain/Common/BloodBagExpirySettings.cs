namespace BloodLineAPI.Domain.Common;

public class BloodBagExpirySettings
{
    public int WholeBloodDays { get; set; } = 42;
    public int PlasmaDays { get; set; } = 365;
    public int PlateletsDays { get; set; } = 5;

    public int GetExpiryDays(DonationType type) => type switch
    {
        DonationType.WholeBlood => WholeBloodDays,
        DonationType.Plasma => PlasmaDays,
        DonationType.Platelets => PlateletsDays,
        _ => WholeBloodDays
    };
}
