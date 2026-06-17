using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Lab.Queries.GetLabDashboardStats;

public sealed class GetLabDashboardStatsQueryHandler : IRequestHandler<GetLabDashboardStatsQuery, GetLabDashboardStatsResult>
{
    private readonly IApplicationDbContext _dbContext;

    public GetLabDashboardStatsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetLabDashboardStatsResult> Handle(GetLabDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var pendingTests = await _dbContext.DonationAppointments
            .AsNoTracking()
            .CountAsync(d => d.BloodBag != null && !d.BloodBag.BloodTestResults.Any(), cancellationToken);

        var completedTests = await _dbContext.DonationAppointments
            .AsNoTracking()
            .CountAsync(d => d.BloodBag != null && d.BloodBag.BloodTestResults.Any(), cancellationToken);

        var totalResults = await _dbContext.BloodTestResults
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var safeResults = await _dbContext.BloodTestResults
            .AsNoTracking()
            .CountAsync(r => r.IsSafe, cancellationToken);

        var rejectedResults = await _dbContext.BloodTestResults
            .AsNoTracking()
            .CountAsync(r => !r.IsSafe, cancellationToken);

        return new GetLabDashboardStatsResult(
            new LabTestsStatsDto(pendingTests, completedTests),
            new LabResultsStatsDto(totalResults, safeResults, rejectedResults)
        );
    }
}
