namespace BloodLineAPI.Application.Features.Lab.Commands.SubmitLabTestResult;

public sealed record SubmitLabTestResultResult(
    Guid DonationAppointmentId,
    string Outcome,
    Guid BloodBagId,
    DateTime CompletedAt,
    Guid CompletedById,
    string CompletedByName);