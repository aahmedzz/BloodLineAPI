using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonationCenters.Dtos;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.DonationCenters.Commands.UpdateMainBranchSettings;

public sealed class UpdateMainBranchSettingsCommandHandler(
    IApplicationDbContext dbContext,
    IAppointmentRealignmentScheduler appointmentRealignmentScheduler)
    : IRequestHandler<UpdateMainBranchSettingsCommand, Result<MainBranchSettingsResult>>
{
    private static readonly Guid BeniSuefMainBranchId = Guid.Parse("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1");

    public async Task<Result<MainBranchSettingsResult>> Handle(
        UpdateMainBranchSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var center = await dbContext.DonationCenters
            .Include(c => c.OpeningHours)
            .Include(c => c.CenterExclusions)
            .FirstOrDefaultAsync(c => c.Id == BeniSuefMainBranchId, cancellationToken);

        if (center == null)
        {
            return Result<MainBranchSettingsResult>.Failure("Main branch center not found.");
        }

        var slotSettingsChanged = center.SlotDurationMinutes != request.SlotDurationMinutes ||
                                  center.MaxDonorsPerSlot != request.MaxDonorsPerSlot;

        // 1. Update basic fields
        center.Name = request.Name.Trim();
        center.Location = request.Location.Trim();
        center.AddressDetails = request.AddressDetails.Trim();
        center.SlotDurationMinutes = request.SlotDurationMinutes;
        center.MaxDonorsPerSlot = request.MaxDonorsPerSlot;

        // 2. Map supported donation types from frontend format to DB format (comma separated)
        var mappedTypes = request.SupportedDonationTypes
            .Select(type => type.ToLowerInvariant().Replace(" ", "") switch
            {
                "whole_blood" => "WholeBlood",
                "plasma" => "Plasma",
                "platelets" => "Platelets",
                _ => type
            });
        center.SupportedDonationTypes = string.Join(',', mappedTypes);

        // 3. Update weekly hours
        // Remove existing weekly hours
        foreach (var oh in center.OpeningHours.ToList())
        {
            dbContext.OpeningHours.Remove(oh);
        }

        // Add new weekly hours
        foreach (var h in request.WeeklyHours)
        {
            var day = (DayOfWeek)h.DayOfWeek;
            TimeSpan openTime = TimeSpan.Zero;
            TimeSpan closeTime = TimeSpan.Zero;

            if (!h.IsClosed)
            {
                openTime = ParseTimeSpan(h.OpeningTime);
                closeTime = ParseTimeSpan(h.ClosingTime);

                // Safety guard: if parsing produced zero/equal times, fall back to center defaults
                // so we never throw a DomainException and roll back the entire save.
                if (openTime == TimeSpan.Zero && closeTime == TimeSpan.Zero)
                {
                    openTime = center.StartTime;
                    closeTime = center.EndTime;
                }
            }

            var openingHours = OpeningHours.Create(
                center.Id,
                day,
                h.IsClosed,
                openTime,
                closeTime,
                h.MaxDonorsPerSlot
            );

            dbContext.OpeningHours.Add(openingHours);
        }

        // 4. Update exclusions
        // Remove existing exclusions
        foreach (var ex in center.CenterExclusions.ToList())
        {
            dbContext.CenterExclusions.Remove(ex);
        }

        // Add new exclusions
        foreach (var e in request.Exclusions)
        {
            var date = DateTime.Parse(e.Date).Date;
            TimeSpan? specialOpen = null;
            TimeSpan? specialClose = null;

            if (!e.IsClosed)
            {
                if (TimeSpan.TryParse(e.SpecialOpeningTime, out var open))
                {
                    specialOpen = open;
                }
                if (TimeSpan.TryParse(e.SpecialClosingTime, out var close))
                {
                    specialClose = close;
                }
            }

            var exclusion = CenterExclusion.Create(
                center.Id,
                date,
                e.IsClosed,
                e.Reason.Trim(),
                specialOpen,
                specialClose
            );

            dbContext.CenterExclusions.Add(exclusion);
        }

        // 5. Save changes
        await dbContext.SaveChangesAsync(cancellationToken);

        // Realign future appointments if slot settings changed (enqueued in background via Hangfire)
        if (slotSettingsChanged)
        {
            appointmentRealignmentScheduler.EnqueueRealignment(
                center.Id,
                center.SlotDurationMinutes ?? 15,
                center.MaxDonorsPerSlot);
        }

        // 6. Map and return updated result
        var supportedTypesResult = center.SupportedDonationTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(type => type switch
            {
                "WholeBlood" => "whole_blood",
                "Plasma" => "plasma",
                "Platelets" => "platelets",
                _ => type.ToLowerInvariant()
            })
            .ToList();

        var weeklyHoursList = new List<WeeklyHourDto>();
        var dayOrder = new[] { 6, 0, 1, 2, 3, 4, 5 }; // Sat, Sun, Mon, Tue, Wed, Thu, Fri
        foreach (var i in dayOrder)
        {
            var dayOfWeek = (DayOfWeek)i;
            var dbHour = dbContext.OpeningHours.Local
                .FirstOrDefault(oh => oh.CenterId == center.Id && oh.DayOfWeek == dayOfWeek)
                ?? center.OpeningHours.FirstOrDefault(oh => oh.DayOfWeek == dayOfWeek);

            if (dbHour != null)
            {
                weeklyHoursList.Add(new WeeklyHourDto(
                    DayOfWeek: i,
                    IsClosed: dbHour.IsClosed,
                    OpeningTime: dbHour.OpeningTime.ToString(@"hh\:mm"),
                    ClosingTime: dbHour.ClosingTime.ToString(@"hh\:mm"),
                    MaxDonorsPerSlot: dbHour.MaxDonorsPerSlot ?? center.MaxDonorsPerSlot
                ));
            }
            else
            {
                weeklyHoursList.Add(new WeeklyHourDto(
                    DayOfWeek: i,
                    IsClosed: true,
                    OpeningTime: "00:00",
                    ClosingTime: "00:00",
                    MaxDonorsPerSlot: center.MaxDonorsPerSlot
                ));
            }
        }

        var exclusionsList = dbContext.CenterExclusions.Local
            .Where(ex => ex.CenterId == center.Id)
            .Concat(center.CenterExclusions)
            .DistinctBy(ex => ex.Id)
            .Select(ex => new ExclusionDto(
                Id: ex.Id,
                Date: ex.Date.ToString("yyyy-MM-dd"),
                IsClosed: ex.IsClosed,
                SpecialOpeningTime: ex.SpecialOpeningTime?.ToString(@"hh\:mm"),
                SpecialClosingTime: ex.SpecialClosingTime?.ToString(@"hh\:mm"),
                Reason: ex.Reason
            ))
            .ToList();

        var result = new MainBranchSettingsResult(
            Id: center.Id,
            Name: center.Name,
            Location: center.Location,
            AddressDetails: center.AddressDetails,
            PhoneNumber: "0822088186", // Hardcoded per user request
            Email: "info@bsgh.gov.eg",  // Hardcoded per user request
            SupportedDonationTypes: supportedTypesResult,
            SlotDurationMinutes: center.SlotDurationMinutes ?? 15,
            MaxDonorsPerSlot: center.MaxDonorsPerSlot,
            WeeklyHours: weeklyHoursList,
            Exclusions: exclusionsList,
            UpdatedAt: (center.LastModifiedAt ?? center.CreatedAt).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Version: request.Version // Return request version to satisfy client optimistic concurrency
        );

        return Result<MainBranchSettingsResult>.Success(result);
    }

    /// <summary>
    /// Parses a time string in either 24-hour ("HH:mm" / "H:mm") or
    /// 12-hour AM/PM ("h:mm tt" / "hh:mm tt") format.
    /// Returns TimeSpan.Zero if parsing fails.
    /// </summary>
    private static TimeSpan ParseTimeSpan(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return TimeSpan.Zero;

        // Try standard 24-hour TimeSpan parsing first (e.g. "08:00", "20:00")
        if (TimeSpan.TryParse(value.Trim(), out var ts))
            return ts;

        // Fall back to 12-hour DateTime parsing (e.g. "08:00 AM", "04:00 PM")
        if (DateTime.TryParseExact(
                value.Trim(),
                new[] { "h:mm tt", "hh:mm tt", "h:mm:ss tt", "hh:mm:ss tt" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
        {
            return dt.TimeOfDay;
        }

        return TimeSpan.Zero;
    }
}
