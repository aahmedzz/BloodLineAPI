namespace BloodLineAPI.Application.Features.Appointments.Commands.SubmitHealthPreScreening;

public sealed record HealthPreScreeningResultDto(
    Guid ScreeningId,
    bool IsEligible,
    string? IneligibilityReason,
    string? Recommendation,
    bool AppointmentCancelled);
