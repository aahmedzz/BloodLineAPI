using BloodLineAPI.Domain.Enums;
using System;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetAllBadges;

public sealed record BadgeDetailsDto(
    Guid Id,
    string BadgeKey,
    string BadgeName,
    string BadgeNameAr,
    string BadgeDescription,
    string BadgeDescriptionAr,
    string IconUrl,
    BadgeType BadgeType,
    int BonusPoints
);
