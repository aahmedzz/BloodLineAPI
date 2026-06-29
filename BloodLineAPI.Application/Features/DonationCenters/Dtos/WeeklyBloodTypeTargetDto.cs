namespace BloodLineAPI.Application.Features.DonationCenters.Dtos
{
    public record WeeklyBloodTypeTargetDto(
        string BloodType,
        int TargetCount,
        int CurrentDonationsCount);
}
