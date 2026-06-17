namespace BloodLineAPI.Application.Features.Lab.Queries.GetLabDashboardStats;

public sealed record LabTestsStatsDto(
    int Pending,
    int Completed
);

public sealed record LabResultsStatsDto(
    int Total,
    int Safe,
    int Rejected
);

public sealed record GetLabDashboardStatsResult(
    LabTestsStatsDto Tests,
    LabResultsStatsDto Results
);
