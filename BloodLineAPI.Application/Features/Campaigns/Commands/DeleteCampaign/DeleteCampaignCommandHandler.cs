using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Application.Features.Campaigns.Commands.DeleteCampaign;

public sealed class DeleteCampaignCommandHandler(
    IApplicationDbContext dbContext,
    ICampaignScheduler campaignScheduler,
    IBackgroundNotificationService backgroundNotificationService,
    IDateTimeProvider dateTimeProvider,
    ILogger<DeleteCampaignCommandHandler> logger)
    : IRequestHandler<DeleteCampaignCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = Guid.TryParse(request.Id, out var parsedGuid)
            ? await dbContext.DonationCenters.FirstOrDefaultAsync(c => c.Id == parsedGuid && c.CenterType == CenterType.Campaign, cancellationToken)
            : await dbContext.DonationCenters.FirstOrDefaultAsync(c => c.CampaignCode == request.Id && c.CenterType == CenterType.Campaign, cancellationToken);

        if (campaign == null)
        {
            throw new NotFoundException(nameof(DonationCenter), request.Id);
        }

        if (campaign.Status == CenterStatus.Completed)
        {
            return Result<Unit>.Failure("FORBIDDEN: لا يمكن حذف حملة مكتملة بالفعل.");
        }

        // 1. Check for confirmed or completed appointments
        var hasConfirmedOrCompleted = await dbContext.DonationAppointments
            .AnyAsync(a => a.DonationCenterId == campaign.Id &&
                           (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Completed),
                      cancellationToken);

        if (hasConfirmedOrCompleted)
        {
            return Result<Unit>.Failure("FORBIDDEN: لا يمكن حذف الحملة لوجود مواعيد مؤكدة أو مكتملة.");
        }

        // 2. Fetch and cancel pending appointments
        var pendingAppointments = await dbContext.DonationAppointments
            .Include(a => a.Donor)
                .ThenInclude(d => d.User)
            .Where(a => a.DonationCenterId == campaign.Id && a.Status == AppointmentStatus.Pending)
            .ToListAsync(cancellationToken);

        var now = dateTimeProvider.LocalNow;
        int cancelledCount = 0;

        var pendingNotifications = new List<(Guid DonorId, string Message, Dictionary<string, string> Payload)>();

        foreach (var appt in pendingAppointments)
        {
            appt.Cancel("تم إلغاء الموعد بسبب حذف الحملة", now, gracePeriodMinutes: 0);
            cancelledCount++;

            var message = $"تم إلغاء موعدك في {campaign.Name} بسبب إلغاء حملة التبرع بالدم.";
            var payload = new Dictionary<string, string>
            {
                ["targetEntity"] = "DonationAppointment",
                ["targetId"] = appt.Id.ToString()
            };

            pendingNotifications.Add((appt.DonorId, message, payload));
        }

        // 3. Unschedule Hangfire jobs
        if (!string.IsNullOrWhiteSpace(campaign.ScheduledJobIds))
        {
            var jobIds = campaign.ScheduledJobIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
            campaignScheduler.UnscheduleJobs(jobIds);
        }

        // 4. Remove campaign
        dbContext.DonationCenters.Remove(campaign);

        // Save changes (cancelling appointments and removing campaign)
        await dbContext.SaveChangesAsync(cancellationToken);

        // Queue cancellation notifications in the background
        foreach (var (donorId, message, payload) in pendingNotifications)
        {
            try
            {
                backgroundNotificationService.EnqueueNotification(
                    donorId,
                    "إلغاء موعد التبرع",
                    message,
                    NotificationType.AppointmentCancelled,
                    payload);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to enqueue cancellation notification to donor {DonorId} during campaign deletion.", donorId);
            }
        }

        logger.LogInformation("Campaign {CampaignId} ({CampaignCode}) deleted. {CancelledCount} pending appointments cancelled.",
            campaign.Id, campaign.CampaignCode, cancelledCount);

        return Result<Unit>.Success(Unit.Value);
    }
}
