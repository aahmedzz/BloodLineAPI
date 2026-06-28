using BloodLineAPI.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class ResetMonthlyPointsJob(
    IApplicationDbContext dbContext,
    ILogger<ResetMonthlyPointsJob> logger)
{
    /// <summary>
    /// Resets MonthlyPoints to 0 for all donors at the start of every calendar month.
    /// Uses Entity Framework Core's high-performance bulk update feature.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting ResetMonthlyPointsJob: Resetting monthly points for all donors.");

        try
        {
            var affectedRows = await dbContext.Donors
                .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.MonthlyPoints, 0), ct);

            logger.LogInformation("ResetMonthlyPointsJob completed successfully. Reset monthly points for {Count} donors.", affectedRows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during ResetMonthlyPointsJob execution.");
            throw;
        }
    }
}
