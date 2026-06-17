using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.DonorEligibility.Dtos;

public record SendBulkNotificationResultDto(
    int Requested,
    int Sent,
    int Failed,
    IReadOnlyList<Guid> FailedDonorIds);
