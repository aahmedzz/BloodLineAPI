using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.DonorEligibility.Dtos;

public record FailedDonorDto(
    Guid DonorId,
    string FullName,
    string PhoneNumber,
    string BloodType,
    string FailureReason);

public record SendBulkNotificationResultDto(
    Guid? AppealId,
    int Requested,
    int Sent,
    int Failed,
    IReadOnlyList<FailedDonorDto> FailedDonors);
