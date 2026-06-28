using BloodLineAPI.Domain.Enums;
using System;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetDonorBadgeHistory;

public sealed record BadgeHistoryItemDto(
    Guid Id,
    string BadgeKey,
    string BadgeName,
    string BadgeNameAr,
    string BadgeDescription,
    string BadgeDescriptionAr,
    string IconUrl,
    BadgeType BadgeType,
    int BonusPoints,
    bool IsProgressive,
    string Status, // "Unlocked", "InProgress", "Locked"
    int CurrentProgress,
    int TargetProgress,
    DateTime? UnlockedDate
);
