using System;
using System.Collections.Generic;
using System.Linq;
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
    INotificationService notificationService,
    IDateTimeProvider dateTimeProvider,
    ILogger<CompleteCampaignCommandHandler> logger)
    : IRequestHandler<CompleteCampaignCommand, Result<CampaignDto>>
{
    public async Task<Result<CampaignDto>> Handle(CompleteCampaignCommand request, CancellationToken cancellationToken)
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
            .Where(a => a.DonationCenterId == campaign.Id)
            .Where(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            .ToListAsync(cancellationToken);

        var cancelledDonorIds = new List<(Guid DonorId, Guid AppointmentId)>();

        foreach (var appt in appointmentsToCancel)
        {
            var appointmentStart = appt.ScheduledDate.Date.Add(appt.StartTime);
            if (appointmentStart > now)
            {
                appt.Cancel("تم إلغاء الموعد بسبب إنهاء الحملة", now, gracePeriodMinutes: 0);
                cancelledDonorIds.Add((appt.DonorId, appt.Id));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 4. Send Push Notifications via NotificationService
        foreach (var (donorId, appointmentId) in cancelledDonorIds)
        {
            try
            {
                await notificationService.SendNotificationAsync(
                    donorId,
                    "إلغاء موعد التبرع",
                    $"تم إلغاء موعدك في {campaign.Name} بسبب إنهاء حملة التبرع.",
                    NotificationType.AppointmentCancelled,
                    new Dictionary<string, string>
                    {
                        ["targetEntity"] = "DonationAppointment",
                        ["targetId"] = appointmentId.ToString()
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send cancellation notification to donor {DonorId} during manual early campaign completion.", donorId);
            }
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
            Id: campaign.Id.ToString(),
            CampaignCode: campaign.CampaignCode ?? $"CAM-{campaign.CampaignNumber:D3}",
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
