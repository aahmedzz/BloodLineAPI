namespace BloodLineAPI.Application.Features.Lab.Queries.GetLabTests;

public sealed record LabTestResultDto(
    string Outcome,
    string ConfirmedBloodType,
    string Hcv,
    string Hbv,
    string Syphilis,
    string Hiv,
    string? Notes,
    DateTime CompletedAt,
    Guid CompletedById,
    string CompletedByName
);

public sealed record LabTestDto(
    Guid Id,
    Guid DonorId,
    string DonorName,
    string DonationCode,
    string BloodType,
    string DonationType,
    string? City,
    DateTime RequestedAt,
    string Status,
    LabTestResultDto? Result
);

public sealed record GetLabTestsResult(IEnumerable<LabTestDto> Items, int Total, int Page, int Limit);