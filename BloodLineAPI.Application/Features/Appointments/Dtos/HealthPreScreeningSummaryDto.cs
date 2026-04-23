namespace BloodLineAPI.Application.Features.Appointments.Dtos;

public sealed record HealthPreScreeningSummaryDto(
    Guid Id,
    bool IsEligible,
    DateTime ScreenedAt);
