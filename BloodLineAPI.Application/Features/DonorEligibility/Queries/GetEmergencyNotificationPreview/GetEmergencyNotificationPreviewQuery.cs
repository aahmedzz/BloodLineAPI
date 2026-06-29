using System;
using System.Collections.Generic;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.DonorEligibility.Queries.GetEmergencyNotificationPreview;

public record GetEmergencyNotificationPreviewQuery(
    string? SelectionMode,
    List<Guid>? DonorIds,
    DonorEligibilityFiltersDto? Filters,
    List<Guid>? ExcludedDonorIds) : IRequest<Result<NotificationPreviewResponseDto>>;
