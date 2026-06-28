using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetAllBadges;

public sealed record BadgeListItemDto(
    Guid Id,
    string BadgeKey,
    string BadgeName,
    string BadgeNameAr,
    string BadgeDescription,
    string IconUrl,
    BadgeType BadgeType,
    int BonusPoints,
    bool IsProgressive,
    string Status, // "Unlocked", "InProgress", "Locked"
    int CurrentProgress,
    int TargetProgress,
    DateTime? UnlockedDate);
