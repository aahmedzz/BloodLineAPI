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
    INotificationSender notificationSender,
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
        var notifications = new List<Notification>();
        var notificationTasks = new List<(Guid DonorId, string Title, string Message, Notification Notif)>();

        foreach (var appt in pendingAppointments)
        {
            appt.Cancel("تم إلغاء الموعد بسبب حذف الحملة", now, gracePeriodMinutes: 0);

            var title = "إلغاء موعد التبرع";
            var message = $"تم إلغاء موعدك في {campaign.Name} بسبب إلغاء حملة التبرع بالدم.";
            var payload = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["targetEntity"] = "DonationAppointment",
                ["targetId"] = appt.Id.ToString()
            });

            var notification = new Notification
            {
                UserId = appt.Donor.User.Id,
                Title = title,
                Message = message,
                Type = NotificationType.AppointmentCancelled,
                ActionPayload = payload,
                SentDate = dateTimeProvider.UtcNow,
                IsSent = false
            };

            notifications.Add(notification);
            notificationTasks.Add((appt.DonorId, title, message, notification));
        }

        if (notifications.Any())
        {
            dbContext.Notifications.AddRange(notifications);
        }

        // 3. Unschedule Hangfire jobs
        if (!string.IsNullOrWhiteSpace(campaign.ScheduledJobIds))
        {
            var jobIds = campaign.ScheduledJobIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
            campaignScheduler.UnscheduleJobs(jobIds);
        }

        // 4. Remove campaign
        dbContext.DonationCenters.Remove(campaign);

        // Save changes (cancelling appointments, adding notifications, and removing campaign)
        await dbContext.SaveChangesAsync(cancellationToken);

        // 5. Send push notifications in background
        foreach (var task in notificationTasks)
        {
            try
            {
                var sent = await notificationSender.SendAsync(task.DonorId, task.Title, task.Message, cancellationToken);
                if (sent)
                {
                    task.Notif.IsSent = true;
                    task.Notif.SentVia = "fcm";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send push notification to donor {DonorId} for cancelled appointment during campaign deletion.", task.DonorId);
            }
        }

        if (notificationTasks.Any(t => t.Notif.IsSent))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Campaign {CampaignId} ({CampaignCode}) deleted. {CancelledCount} pending appointments cancelled.",
            campaign.Id, campaign.CampaignCode, notifications.Count);

        return Result<Unit>.Success(Unit.Value);
    }
}
