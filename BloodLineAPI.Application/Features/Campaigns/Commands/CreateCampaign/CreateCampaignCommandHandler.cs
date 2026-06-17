using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using BloodLineAPI.Application.Features.Campaigns.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Campaigns.Commands.CreateCampaign;

public sealed class CreateCampaignCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ICampaignScheduler campaignScheduler,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CreateCampaignCommand, Result<CampaignDto>>
{
    public async Task<Result<CampaignDto>> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve Creator details from Current User and Staff lookup
        var currentUserId = currentUserService.UserId;
        string createdByName = "System";
        Guid? createdById = null;

        if (!string.IsNullOrEmpty(currentUserId))
        {
            createdById = Guid.Parse(currentUserId);
            var staff = await dbContext.Staff
                .FirstOrDefaultAsync(s => s.Id == createdById, cancellationToken);
            if (staff != null)
            {
                createdByName = staff.FullName;
            }
        }

        // 2. Parse times
        var startTime = TimeSpan.Parse(request.StartTime);
        var endTime = TimeSpan.Parse(request.EndTime);

        // 3. Generate scheduled dates
        var today = dateTimeProvider.CurrentLocalDate;
        var generatedDates = new List<DateOnly>();

        var recurrenceEnabled = request.Recurrence?.Enabled ?? false;
        var recurrenceGroupId = recurrenceEnabled ? Guid.NewGuid() : (Guid?)null;

        if (!recurrenceEnabled || request.Recurrence?.Type == "none")
        {
            generatedDates.Add(today);
        }
        else
        {
            var type = request.Recurrence!.Type.ToLower();
            // Cap recurrence end date to 3 months from today if null or too far
            var maxCapDate = today.AddMonths(3);
            var recurrenceEndDate = maxCapDate;

            if (!string.IsNullOrEmpty(request.Recurrence.EndDate) && DateOnly.TryParse(request.Recurrence.EndDate, out var parsedEnd))
            {
                if (parsedEnd < maxCapDate)
                {
                    recurrenceEndDate = parsedEnd;
                }
            }

            var currentDate = today;
            while (currentDate <= recurrenceEndDate)
            {
                bool match = false;
                switch (type)
                {
                    case "daily":
                        match = true;
                        break;

                    case "weekly":
                        match = currentDate.DayOfWeek == today.DayOfWeek;
                        break;

                    case "monthly":
                        match = currentDate.Day == today.Day;
                        break;

                    case "custom":
                        if (request.Recurrence.WeekDays != null && request.Recurrence.WeekDays.Any())
                        {
                            var currentDayIdx = (int)currentDate.DayOfWeek;
                            match = request.Recurrence.WeekDays.Contains(currentDayIdx);
                        }
                        break;
                }

                if (match)
                {
                    generatedDates.Add(currentDate);
                }

                currentDate = currentDate.AddDays(1);
            }
        }

        if (!generatedDates.Any())
        {
            return Result<CampaignDto>.Failure("لم يتم إنشاء أي تواريخ صالحة بناءً على نمط التكرار المحدد.");
        }

        var firstDate = generatedDates.First();
        var lastDate = generatedDates.Last();
        var nowLocal = dateTimeProvider.LocalNow;

        // Determine initial status based on all dates in the series
        var isCurrentlyActive = false;
        var hasPassedAll = true;

        foreach (var date in generatedDates)
        {
            var localStart = date.ToDateTime(TimeOnly.FromTimeSpan(startTime));
            var localEnd = date.ToDateTime(TimeOnly.FromTimeSpan(endTime));
            if (endTime < startTime)
            {
                localEnd = localEnd.AddDays(1);
            }

            if (nowLocal >= localStart && nowLocal <= localEnd)
            {
                isCurrentlyActive = true;
            }
            if (nowLocal <= localEnd)
            {
                hasPassedAll = false;
            }
        }

        var status = CenterStatus.NotActive;
        if (isCurrentlyActive)
        {
            status = CenterStatus.Active;
        }
        else if (hasPassedAll)
        {
            status = CenterStatus.Completed;
        }

        // 4. Create single DonationCenter record
        var campaign = new DonationCenter
        {
            Id = Guid.NewGuid(),
            Name = request.Title,
            Location = request.City,
            AddressDetails = request.City,
            Latitude = request.Latitude ?? 0,
            Longitude = request.Longitude ?? 0,
            CenterType = CenterType.Campaign,
            Status = status,
            SupportedDonationTypes = string.Join(',', request.AvailableDonationTypes.Select(t => Enum.Parse<DonationType>(t, true).ToString())),
            StartDate = firstDate.ToDateTime(TimeOnly.MinValue),
            EndDate = lastDate.ToDateTime(TimeOnly.MinValue),
            StartTime = startTime,
            EndTime = endTime,
            DescriptionText = request.Description,
            MaxDonorsPerSlot = request.SlotCapacity,
            SlotDurationMinutes = request.SlotDuration,
            TargetDonors = request.TargetDonors,
            CreatedById = createdById,
            CreatedByName = createdByName,
            RecurrenceEnabled = recurrenceEnabled,
            RecurrenceType = recurrenceEnabled ? Enum.Parse<RecurrenceType>(request.Recurrence!.Type, true) : RecurrenceType.None,
            RecurrenceWeekDays = recurrenceEnabled && request.Recurrence?.WeekDays != null
                ? string.Join(",", request.Recurrence.WeekDays)
                : null,
            RecurrenceEndDate = recurrenceEnabled && request.Recurrence?.EndDate != null
                ? DateOnly.Parse(request.Recurrence.EndDate).ToDateTime(TimeOnly.MinValue)
                : null,
            RecurrenceGroupId = recurrenceGroupId,
            CreatedAt = nowLocal,
            CreatedBy = currentUserId
        };

        dbContext.DonationCenters.Add(campaign);

        // Save once to persist record and generate CampaignCode / CampaignNumber
        await dbContext.SaveChangesAsync(cancellationToken);

        // 5. Schedule Hangfire jobs for each operating date
        var jobIds = new List<string>();

        for (int i = 0; i < generatedDates.Count; i++)
        {
            var date = generatedDates[i];
            var isFinalDay = (i == generatedDates.Count - 1);

            var localStart = date.ToDateTime(TimeOnly.FromTimeSpan(campaign.StartTime));
            var localEnd = date.ToDateTime(TimeOnly.FromTimeSpan(campaign.EndTime));
            if (campaign.EndTime < campaign.StartTime)
            {
                localEnd = localEnd.AddDays(1);
            }

            // Schedule activation if starting in future
            if (nowLocal < localStart)
            {
                var actJobId = campaignScheduler.ScheduleActivation(campaign.Id, localStart);
                if (!string.IsNullOrEmpty(actJobId))
                {
                    jobIds.Add(actJobId);
                }

                // Schedule deactivation or completion at the end of the operating slot
                if (isFinalDay)
                {
                    var compJobId = campaignScheduler.ScheduleCompletion(campaign.Id, localEnd);
                    if (!string.IsNullOrEmpty(compJobId)) jobIds.Add(compJobId);
                }
                else
                {
                    var deactJobId = campaignScheduler.ScheduleDeactivation(campaign.Id, localEnd);
                    if (!string.IsNullOrEmpty(deactJobId)) jobIds.Add(deactJobId);
                }
            }
            // If currently running in this day's slot
            else if (nowLocal >= localStart && nowLocal <= localEnd)
            {
                if (isFinalDay)
                {
                    var compJobId = campaignScheduler.ScheduleCompletion(campaign.Id, localEnd);
                    if (!string.IsNullOrEmpty(compJobId)) jobIds.Add(compJobId);
                }
                else
                {
                    var deactJobId = campaignScheduler.ScheduleDeactivation(campaign.Id, localEnd);
                    if (!string.IsNullOrEmpty(deactJobId)) jobIds.Add(deactJobId);
                }
            }
        }

        if (jobIds.Any())
        {
            campaign.ScheduledJobIds = string.Join(",", jobIds);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 6. Map and return the campaign DTO
        var recurrenceDto = recurrenceEnabled ? new RecurrenceSettingsDto(
            true,
            request.Recurrence!.Type,
            request.Recurrence.WeekDays,
            request.Recurrence.EndDate
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
            RegisteredDonors: 0,
            AppointmentsCount: 0,
            Status: campaign.Status.ToString().ToLowerInvariant(),
            CreatedBy: campaign.CreatedById?.ToString() ?? string.Empty,
            CreatedByName: campaign.CreatedByName ?? string.Empty,
            Description: campaign.DescriptionText ?? string.Empty,
            Recurrence: recurrenceDto,
            AvailableDonationTypes: campaign.GetSupportedDonationTypes()
        );

        return Result<CampaignDto>.Success(resultDto);
    }
}
