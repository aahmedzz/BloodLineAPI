using MediatR;

namespace BloodLineAPI.Application.Features.Lab.Commands.SubmitLabTestResult;

public sealed record SubmitLabTestResultCommand(
    Guid DonationAppointmentId,
    string ConfirmedBloodType,
    string Hcv,
    string Hbv,
    string Syphilis,
    string Hiv,
    string? Notes) : IRequest<SubmitLabTestResultResult>;