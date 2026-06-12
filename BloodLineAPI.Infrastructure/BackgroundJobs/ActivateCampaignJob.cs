using System;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class ActivateCampaignJob(
    IApplicationDbContext dbContext,
    ILogger<ActivateCampaignJob> logger)
{
    public async Task ExecuteAsync(Guid campaignId, CancellationToken ct = default)
    {
        var campaign = await dbContext.DonationCenters
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct);

        if (campaign == null)
        {
            logger.LogWarning("Campaign {CampaignId} not found for activation.", campaignId);
            return;
        }

        if (campaign.CenterType != CenterType.Campaign)
        {
            logger.LogWarning("Center {CenterId} is not a Campaign. Status update skipped.", campaignId);
            return;
        }

        if (campaign.Status == CenterStatus.NotActive)
        {
            campaign.Status = CenterStatus.Active;
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Campaign {CampaignId} ({CampaignCode}) has been successfully activated.", campaignId, campaign.CampaignCode);
        }
        else
        {
            logger.LogInformation("Campaign {CampaignId} status is already {Status}; skipping activation.", campaignId, campaign.Status);
        }
    }
}
