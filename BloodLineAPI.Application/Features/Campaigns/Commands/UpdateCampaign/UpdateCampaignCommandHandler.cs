using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using BloodLineAPI.Application.Features.Campaigns.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Campaigns.Commands.UpdateCampaign;

public sealed class UpdateCampaignCommandHandler(
    IApplicationDbContext dbContext,
    ICampaignScheduler campaignScheduler,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<UpdateCampaignCommand, Result<CampaignDto>>
{
    public async Task<Result<CampaignDto>> Handle(UpdateCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await dbContext.DonationCenters
            .FirstOrDefaultAsync(c => c.CampaignCode == request.Id && c.CenterType == CenterType.Campaign, cancellationToken)
            ?? throw new NotFoundException(nameof(DonationCenter), request.Id);

        if (campaign.Status == CenterStatus.Completed)
        {
            return Result<CampaignDto>.Failure("FORBIDDEN: لا يمكن تعديل حملة مكتملة بالفعل.");
        }

        // Track if schedule is changing
        bool scheduleChanged = false;
        var nowLocal = dateTimeProvider.LocalNow;

        if (request.Title != null) campaign.Name = request.Title;
        if (request.City != null)
        {
            campaign.Location = request.City;
            campaign.AddressDetails = request.City;
        }
        if (request.Latitude.HasValue) campaign.Latitude = request.Latitude.Value;
        if (request.Longitude.HasValue) campaign.Longitude = request.Longitude.Value;
        if (request.SlotCapacity.HasValue) campaign.MaxDonorsPerSlot = request.SlotCapacity.Value;
        if (request.SlotDuration.HasValue) campaign.SlotDurationMinutes = request.SlotDuration.Value;
        if (request.TargetDonors.HasValue) campaign.TargetDonors = request.TargetDonors.Value;
        if (request.Description != null) campaign.DescriptionText = request.Description;

        if (request.AvailableDonationTypes != null)
        {
            foreach (var typeStr in request.AvailableDonationTypes)
            {
                if (!Enum.TryParse<DonationType>(typeStr, true, out _))
                {
                    return Result<CampaignDto>.Failure($"نوع تبرع غير صالح: {typeStr}. القيم المسموح بها هي: WholeBlood, Plasma, Platelets");
                }
            }

            campaign.SupportedDonationTypes = string.Join(',', request.AvailableDonationTypes.Select(t => Enum.Parse<DonationType>(t, true).ToString()));
        }

        if (request.StartTime != null)
        {
            var parsedStartTime = TimeSpan.Parse(request.StartTime);
            if (campaign.StartTime != parsedStartTime)
            {
                campaign.StartTime = parsedStartTime;
                scheduleChanged = true;
            }
        }

        if (request.EndTime != null)
        {
            var parsedEndTime = TimeSpan.Parse(request.EndTime);
            if (campaign.EndTime != parsedEndTime)
            {
                campaign.EndTime = parsedEndTime;
                scheduleChanged = true;
            }
        }

        if (request.Recurrence != null)
        {
            var newRecurrenceEnabled = request.Recurrence.Enabled;
            var newRecurrenceType = newRecurrenceEnabled ? Enum.Parse<RecurrenceType>(request.Recurrence.Type, true) : RecurrenceType.None;
            var newRecurrenceWeekDays = newRecurrenceEnabled && request.Recurrence.WeekDays != null ? string.Join(",", request.Recurrence.WeekDays) : null;
            var newRecurrenceEndDate = newRecurrenceEnabled && request.Recurrence.EndDate != null ? (DateTime?)DateOnly.Parse(request.Recurrence.EndDate).ToDateTime(TimeOnly.MinValue) : null;

            if (campaign.RecurrenceEnabled != newRecurrenceEnabled ||
                campaign.RecurrenceType != newRecurrenceType ||
                campaign.RecurrenceWeekDays != newRecurrenceWeekDays ||
                campaign.RecurrenceEndDate != newRecurrenceEndDate)
            {
                campaign.RecurrenceEnabled = newRecurrenceEnabled;
                campaign.RecurrenceType = newRecurrenceType;
                campaign.RecurrenceWeekDays = newRecurrenceWeekDays;
                campaign.RecurrenceEndDate = newRecurrenceEndDate;
                scheduleChanged = true;
            }
        }

        if (scheduleChanged)
        {
            // 1. Unschedule old Hangfire jobs
            if (!string.IsNullOrWhiteSpace(campaign.ScheduledJobIds))
            {
                var oldJobIds = campaign.ScheduledJobIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                campaignScheduler.UnscheduleJobs(oldJobIds);
                campaign.ScheduledJobIds = null;
            }

            // 2. Generate operating dates starting from original campaign.StartDate
            var seriesStartDate = DateOnly.FromDateTime(campaign.StartDate);
            var generatedDates = new List<DateOnly>();

            if (!campaign.RecurrenceEnabled || campaign.RecurrenceType == RecurrenceType.None)
            {
                generatedDates.Add(seriesStartDate);
            }
            else
            {
                var type = (campaign.RecurrenceType ?? RecurrenceType.None).ToString().ToLowerInvariant();
                var maxCapDate = seriesStartDate.AddMonths(3);
                var recurrenceEndDate = maxCapDate;

                if (campaign.RecurrenceEndDate.HasValue)
                {
                    var parsedEnd = DateOnly.FromDateTime(campaign.RecurrenceEndDate.Value);
                    if (parsedEnd < maxCapDate)
                    {
                        recurrenceEndDate = parsedEnd;
                    }
                }

                var currentDate = seriesStartDate;
                while (currentDate <= recurrenceEndDate)
                {
                    bool match = false;
                    switch (type)
                    {
                        case "daily":
                            match = true;
                            break;

                        case "weekly":
                            match = currentDate.DayOfWeek == seriesStartDate.DayOfWeek;
                            break;

                        case "monthly":
                            match = currentDate.Day == seriesStartDate.Day;
                            break;

                        case "custom":
                            if (campaign.RecurrenceWeekDays != null)
                            {
                                var allowedDays = campaign.RecurrenceWeekDays.Split(',').Select(int.Parse).ToList();
                                var currentDayIdx = (int)currentDate.DayOfWeek;
                                match = allowedDays.Contains(currentDayIdx);
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

            if (generatedDates.Any())
            {
                // Update EndDate to last generated date
                campaign.EndDate = generatedDates.Last().ToDateTime(TimeOnly.MinValue);

                // 3. Determine status dynamically
                var isCurrentlyActive = false;
                var hasPassedAll = true;

                foreach (var date in generatedDates)
                {
                    var localStart = date.ToDateTime(TimeOnly.FromTimeSpan(campaign.StartTime));
                    var localEnd = date.ToDateTime(TimeOnly.FromTimeSpan(campaign.EndTime));
                    if (campaign.EndTime < campaign.StartTime)
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

                if (isCurrentlyActive)
                {
                    campaign.Status = CenterStatus.Active;
                }
                else if (hasPassedAll)
                {
                    campaign.Status = CenterStatus.Completed;
                }
                else
                {
                    campaign.Status = CenterStatus.NotActive;
                }

                // 4. Schedule new Hangfire jobs
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

                    if (nowLocal < localStart)
                    {
                        var actJobId = campaignScheduler.ScheduleActivation(campaign.Id, localStart);
                        if (!string.IsNullOrEmpty(actJobId)) jobIds.Add(actJobId);

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
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Fetch counts for mapping
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

        return Result<CampaignDto>.Success(resultDto);
    }
}
