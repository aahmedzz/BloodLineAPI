using System;
using System.Collections.Generic;
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

namespace BloodLineAPI.Application.Features.DonationCenters.Queries.GetMainBranchSettings;

public sealed class GetMainBranchSettingsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMainBranchSettingsQuery, Result<MainBranchSettingsResult>>
{
    private static readonly Guid BeniSuefMainBranchId = Guid.Parse("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1");

    public async Task<Result<MainBranchSettingsResult>> Handle(
        GetMainBranchSettingsQuery request,
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

        // Map supported donation types from database format (e.g. "WholeBlood,Plasma") to frontend format (e.g. "whole_blood")
        var supportedTypes = center.SupportedDonationTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(type => type switch
            {
                "WholeBlood" => "whole_blood",
                "Plasma" => "plasma",
                "Platelets" => "platelets",
                _ => type.ToLowerInvariant()
            })
            .ToList();

        // Ensure we always return exactly 7 days in Saturday-first order
        // DayOfWeek int values stay as .NET standard: 0=Sun, 1=Mon, ..., 6=Sat
        var dayOrder = new[] { 6, 0, 1, 2, 3, 4, 5 }; // Sat, Sun, Mon, Tue, Wed, Thu, Fri
        var weeklyHoursList = new List<WeeklyHourDto>();
        foreach (var i in dayOrder)
        {
            var dayOfWeek = (DayOfWeek)i;
            var dbHour = center.OpeningHours.FirstOrDefault(oh => oh.DayOfWeek == dayOfWeek);

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

        // Map exclusions
        var exclusionsList = center.CenterExclusions
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
            SupportedDonationTypes: supportedTypes,
            SlotDurationMinutes: center.SlotDurationMinutes ?? 15,
            MaxDonorsPerSlot: center.MaxDonorsPerSlot,
            WeeklyHours: weeklyHoursList,
            Exclusions: exclusionsList,
            UpdatedAt: (center.LastModifiedAt ?? center.CreatedAt).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Version: 1 // Dummy version 1 as approved
        );

        return Result<MainBranchSettingsResult>.Success(result);
    }
}
