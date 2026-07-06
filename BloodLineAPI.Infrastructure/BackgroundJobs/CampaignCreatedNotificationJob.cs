using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class CampaignCreatedNotificationJob(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IDynamicSettingsService dynamicSettingsService,
    IBackgroundNotificationService backgroundNotificationService,
    ILogger<CampaignCreatedNotificationJob> logger)
{
    public async Task ExecuteAsync(Guid campaignId, CancellationToken ct = default)
    {
        // 1. Fetch the campaign
        var campaign = await dbContext.DonationCenters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct);

        if (campaign == null || campaign.CenterType != CenterType.Campaign)
        {
            logger.LogWarning("Campaign {CampaignId} not found or is not a campaign.", campaignId);
            return;
        }

        // If coordinates are 0, we can't calculate distance
        if (campaign.Latitude == 0 && campaign.Longitude == 0)
        {
            logger.LogInformation("Campaign {CampaignId} has no valid coordinates. Skipping location-based notifications.", campaignId);
            return;
        }

        // 2. Fetch eligibility settings
        var settings = await dynamicSettingsService.GetSettingsAsync(ct);
        var maleDays = settings.WholeBloodMaleDays;
        var femaleDays = settings.WholeBloodFemaleDays;
        var todayLocal = dateTimeProvider.LocalNow.Date;

        // 3. Define the bounding box for 10 km
        double distanceKm = 10.0;
        double deltaLat = distanceKm / 111.0;
        double deltaLon = distanceKm / (111.0 * Math.Cos(campaign.Latitude * Math.PI / 180.0));

        double minLat = campaign.Latitude - deltaLat;
        double maxLat = campaign.Latitude + deltaLat;
        double minLon = campaign.Longitude - deltaLon;
        double maxLon = campaign.Longitude + deltaLon;

        // 4. Fetch candidate donors who are eligible to donate
        var candidateDonors = await dbContext.Donors
            .AsNoTracking()
            .Where(d => d.Latitude.HasValue && d.Longitude.HasValue)
            .Where(d => d.Latitude >= minLat && d.Latitude <= maxLat && d.Longitude >= minLon && d.Longitude <= maxLon)
            .Where(d => d.Status == DonorStatus.Eligible)
            .Where(d => d.LastDonationDate == null ||
                        d.LastDonationDate.Value.AddDays(d.Gender == Gender.Male ? maleDays : femaleDays) <= todayLocal)
            .Select(d => new { d.Id, d.Latitude, d.Longitude })
            .ToListAsync(ct);

        var notifiedDonorIds = new List<Guid>();

        // 5. In-memory exact Haversine filter
        foreach (var donor in candidateDonors)
        {
            var distance = CalculateDistanceInKm(
                campaign.Latitude,
                campaign.Longitude,
                donor.Latitude!.Value,
                donor.Longitude!.Value);

            if (distance <= distanceKm)
            {
                notifiedDonorIds.Add(donor.Id);
            }
        }

        if (notifiedDonorIds.Count == 0)
        {
            logger.LogInformation("No eligible donors found within {DistanceKm} km of campaign {CampaignId}.", distanceKm, campaignId);
            return;
        }

        // 6. Enqueue notifications in batch
        var title = "📍 حملة تبرع بالدم بالقرب منك!";
        var message = $"عزيزي المتبرع، هناك حملة تبرع بالدم ({campaign.Name}) تقام بالقرب منك في {campaign.Location} بتاريخ {campaign.StartDate:yyyy-MM-dd} من الساعة {campaign.StartTime:hh\\:mm} حتى {campaign.EndTime:hh\\:mm}. تفضل بزيارتنا للمساهمة في إنقاذ الأرواح!";
        
        var payload = new Dictionary<string, string>
        {
            ["targetEntity"] = "DonationCenter",
            ["targetId"] = campaign.Id.ToString()
        };

        backgroundNotificationService.EnqueueBatchNotification(
            notifiedDonorIds,
            title,
            message,
            NotificationType.NewCampaignNearby,
            payload);

        logger.LogInformation("Successfully enqueued notifications to {Count} donors near campaign {CampaignId}.", notifiedDonorIds.Count, campaignId);
    }

    private static double CalculateDistanceInKm(double lat1, double lon1, double lat2, double lon2)
    {
        var r = 6371; // Earth's radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        return r * c;
    }

    private static double ToRadians(double val) => (Math.PI / 180) * val;
}
