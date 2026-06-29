namespace BloodLineAPI.Application.Features.DonationCenters.Dtos
{
    public record UpdateWeeklyBloodTypeTargetDto(
        string BloodType,
        int TargetCount);
}
