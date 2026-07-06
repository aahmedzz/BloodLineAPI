using BloodLineAPI.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Gamification.Commands.ReconcileBadges;

public record ReconcileBadgesCommand : IRequest<Result<ReconcileBadgesResultDto>>;

public record ReconcileBadgesResultDto(
    int TotalDonorsChecked,
    int TotalDonorsUpdated,
    int TotalDonationCountsCorrected,
    int TotalBadgesAwarded,
    int TotalBadgesRemoved,
    int TotalPointsAwarded,
    int TotalPointsDeducted,
    int TotalDonationPointsAwarded,
    List<DonorReconciliationDetailDto> Details);

public record DonorReconciliationDetailDto(
    Guid DonorId,
    string DonorName,
    int PreviousDonationCount,
    int CorrectedDonationCount,
    int BadgesAwardedCount,
    int BadgesRemovedCount,
    int PointsAwardedCount,
    int PointsDeductedCount,
    int DonationPointsAwardedCount,
    List<string> AwardedBadgeKeys,
    List<string> RemovedBadgeKeys);
