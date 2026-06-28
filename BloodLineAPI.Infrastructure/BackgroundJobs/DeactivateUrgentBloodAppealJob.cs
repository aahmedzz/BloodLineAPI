using System;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class DeactivateUrgentBloodAppealJob(
    IApplicationDbContext dbContext,
    ILogger<DeactivateUrgentBloodAppealJob> logger)
{
    public async Task ExecuteAsync(Guid appealId, CancellationToken ct = default)
    {
        var appeal = await dbContext.UrgentBloodAppeals
            .FirstOrDefaultAsync(a => a.Id == appealId, ct);

        if (appeal == null)
        {
            logger.LogWarning("UrgentBloodAppeal {AppealId} not found for deactivation.", appealId);
            return;
        }

        if (appeal.IsActive)
        {
            appeal.IsActive = false;
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("UrgentBloodAppeal {AppealId} has been successfully deactivated automatically.", appealId);
        }
        else
        {
            logger.LogInformation("UrgentBloodAppeal {AppealId} is already inactive.", appealId);
        }
    }
}
