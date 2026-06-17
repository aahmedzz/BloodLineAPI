using System;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class DeactivateCampaignJob(
    IApplicationDbContext dbContext,
    ILogger<DeactivateCampaignJob> logger)
{
    public async Task ExecuteAsync(Guid campaignId, CancellationToken ct = default)
    {
        var campaign = await dbContext.DonationCenters
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct);

        if (campaign == null)
        {
            logger.LogWarning("Campaign {CampaignId} not found for deactivation.", campaignId);
            return;
        }

        if (campaign.CenterType != CenterType.Campaign)
        {
            logger.LogWarning("Center {CenterId} is not a Campaign. Status update skipped.", campaignId);
            return;
        }

        if (campaign.Status == CenterStatus.Active)
        {
            campaign.Status = CenterStatus.NotActive;
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Campaign {CampaignId} ({CampaignCode}) has been successfully deactivated (set to NotActive).", campaignId, campaign.CampaignCode);
        }
        else
        {
            logger.LogInformation("Campaign {CampaignId} status is {Status}; skipping deactivation.", campaignId, campaign.Status);
        }
    }
}
