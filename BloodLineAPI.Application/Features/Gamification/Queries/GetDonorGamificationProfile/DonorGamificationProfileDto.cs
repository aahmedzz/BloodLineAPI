namespace BloodLineAPI.Application.Features.Gamification.Queries.GetDonorGamificationProfile;

public sealed record DonorGamificationProfileDto(
    Guid DonorId,
    string FullName,
    int TotalPoints,
    int MonthlyPoints,
    int TotalDonationCount,
    int? MonthlyRank,
    int? AllTimeRank,
    IReadOnlyList<DonorBadgeDto> Badges);

public sealed record DonorBadgeDto(
    string BadgeKey,
    string BadgeName,
    string BadgeNameAr,
    string IconUrl,
    DateTime EarnedDate,
    int BonusPoints);
