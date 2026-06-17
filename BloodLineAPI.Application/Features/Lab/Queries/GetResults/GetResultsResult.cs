namespace BloodLineAPI.Application.Features.Lab.Queries.GetResults;

public sealed record TestResultDto(
    Guid Id,
    Guid SampleId,
    string DonationCode,
    string DonorName,
    string NationalId,
    string BloodType,
    string ConfirmedBloodType,
    string Hcv,
    string Hbv,
    string Syphilis,
    string Hiv,
    string Outcome,
    string LabDoctor,
    DateTime Date,
    string? Notes
);

public sealed record GetResultsResult(IEnumerable<TestResultDto> Items, int Total, int Page, int Limit);