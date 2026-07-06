using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.DonorEligibility.Dtos;

public record NotificationPreviewResponseDto(
    string Title,
    string Message,
    int RecipientCount,
    int FailedCount,
    IReadOnlyList<FailedDonorDto> FailedDonors);
