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
using BloodLineAPI.Application.Features.Campaigns.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Application.Features.Campaigns.Commands.CompleteCampaign;

public sealed class CompleteCampaignCommandHandler(
    IApplicationDbContext dbContext,
    ICampaignScheduler campaignScheduler,
    INotificationSender notificationSender,
    IDateTimeProvider dateTimeProvider,
    ILogger<CompleteCampaignCommandHandler> logger)
    : IRequestHandler<CompleteCampaignCommand, Result<CampaignDto>>
{
    public async Task<Result<CampaignDto>> Handle(CompleteCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await dbContext.DonationCenters
            .FirstOrDefaultAsync(c => c.CampaignCode == request.Id && c.CenterType == CenterType.Campaign, cancellationToken)
            ?? throw new NotFoundException(nameof(DonationCenter), request.Id);

        if (campaign.Status == CenterStatus.Completed)
        {
            return Result<CampaignDto>.Failure("حملة التبرع مكتملة بالفعل.");
        }

        // 1. Set status to Completed
        campaign.Status = CenterStatus.Completed;

        // 2. Unschedule scheduled Hangfire jobs
        if (!string.IsNullOrWhiteSpace(campaign.ScheduledJobIds))
        {
            var jobIds = campaign.ScheduledJobIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
            campaignScheduler.UnscheduleJobs(jobIds);
            campaign.ScheduledJobIds = null;
        }

        // 3. Fetch all upcoming pending or confirmed appointments
        var now = dateTimeProvider.LocalNow;
        var appointmentsToCancel = await dbContext.DonationAppointments
            .Include(a => a.Donor)
                .ThenInclude(d => d.User)
            .Where(a => a.DonationCenterId == campaign.Id)
            .Where(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            .ToListAsync(cancellationToken);

        var notifications = new List<Notification>();
        var notificationTasks = new List<(Guid DonorId, string Title, string Message, Notification Notif)>();

        foreach (var appt in appointmentsToCancel)
        {
            var appointmentStart = appt.ScheduledDate.Date.Add(appt.StartTime);
            if (appointmentStart > now)
            {
                appt.Cancel("تم إلغاء الموعد بسبب إنهاء الحملة", now, gracePeriodMinutes: 0);

                var title = "إلغاء موعد التبرع";
                var message = $"تم إلغاء موعدك في {campaign.Name} بسبب إنهاء حملة التبرع.";
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
        }

        if (notifications.Any())
        {
            dbContext.Notifications.AddRange(notifications);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 4. Send Push Notifications
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
                logger.LogError(ex, "Failed to send push notification to donor {DonorId} for cancelled appointment during manual early completion.", task.DonorId);
            }
        }

        if (notificationTasks.Any(t => t.Notif.IsSent))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 5. Fetch counts for mapping
        var registeredDonorsCount = await dbContext.DonationAppointments
            .Where(a => a.DonationCenterId == campaign.Id && a.Status != AppointmentStatus.Cancelled)
            .CountAsync(cancellationToken);

        var appBookingsCount = await dbContext.DonationAppointments
            .Where(a => a.DonationCenterId == campaign.Id && a.Source == DonationSource.MobileApp)
            .CountAsync(cancellationToken);

        var recurrenceDto = campaign.RecurrenceEnabled ? new RecurrenceSettingsDto(
            Enabled: true,
            Type: (campaign.RecurrenceType ?? RecurrenceType.None).ToString().ToLowerInvariant(),
            WeekDays: campaign.RecurrenceWeekDays?.Split(',').Select(int.Parse).ToList(),
            EndDate: campaign.RecurrenceEndDate?.ToString("yyyy-MM-dd")
        ) : null;

        var resultDto = new CampaignDto(
            Id: campaign.CampaignCode ?? $"CAM-{campaign.CampaignNumber:D3}",
            Title: campaign.Name,
            City: campaign.Location,
            Latitude: campaign.Latitude,
            Longitude: campaign.Longitude,
            Date: campaign.StartDate.ToString("yyyy-MM-dd"),
            StartTime: campaign.StartTime.ToString(@"hh\:mm"),
            EndTime: campaign.EndTime.ToString(@"hh\:mm"),
            SlotDuration: campaign.SlotDurationMinutes ?? 15,
            SlotCapacity: campaign.MaxDonorsPerSlot,
            TargetDonors: campaign.TargetDonors ?? 0,
            RegisteredDonors: registeredDonorsCount,
            AppointmentsCount: appBookingsCount,
            Status: campaign.Status.ToString().ToLowerInvariant(),
            CreatedBy: campaign.CreatedById?.ToString() ?? string.Empty,
            CreatedByName: campaign.CreatedByName ?? string.Empty,
            Description: campaign.DescriptionText ?? string.Empty,
            Recurrence: recurrenceDto,
            AvailableDonationTypes: campaign.GetSupportedDonationTypes()
        );

        logger.LogInformation("Campaign {CampaignId} ({CampaignCode}) manually completed early.", campaign.Id, campaign.CampaignCode);

        return Result<CampaignDto>.Success(resultDto);
    }
}
