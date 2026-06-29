using BloodLineAPI.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Gamification.Commands.ReconcileBadges;

public record ReconcileBadgesCommand : IRequest<Result<ReconcileBadgesResultDto>>;

public record ReconcileBadgesResultDto(
    int TotalDonorsChecked,
    int TotalDonorsUpdated,
    int TotalBadgesAwarded,
    int TotalPointsAwarded,
    List<DonorReconciliationDetailDto> Details);

public record DonorReconciliationDetailDto(
    Guid DonorId,
    string DonorName,
    int BadgesAwardedCount,
    int PointsAwardedCount,
    List<string> AwardedBadgeKeys);
