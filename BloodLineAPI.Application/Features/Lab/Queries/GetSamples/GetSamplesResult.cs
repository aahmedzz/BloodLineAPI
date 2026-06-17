namespace BloodLineAPI.Application.Features.Lab.Queries.GetSamples;

public sealed record SampleDto(
    Guid Id,
    string DonationCode,
    string DonorName,
    string BloodType,
    string DonationType,
    DateTime CollectedDate,
    string Status,
    string? LabDoctor,
    string? City
);

public sealed record GetSamplesResult(IEnumerable<SampleDto> Items, int Total, int Page, int Limit);