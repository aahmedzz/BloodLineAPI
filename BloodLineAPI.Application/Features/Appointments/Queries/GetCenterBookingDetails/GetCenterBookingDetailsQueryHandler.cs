using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetCenterBookingDetails;

public sealed class GetCenterBookingDetailsQueryHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetCenterBookingDetailsQuery, BookingDetailsDto?>
{
    public async Task<BookingDetailsDto?> Handle(GetCenterBookingDetailsQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch donation center
        var center = await dbContext.DonationCenters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CenterId, cancellationToken);

        if (center is null) return null;

        // 2. Fetch average rating (null if no ratings exist)
        var averageRating = await dbContext.DonationRatings
            .Where(r => r.DonationAppointment.DonationCenterId == request.CenterId)
            .AverageAsync(r => (double?)r.StarScore, cancellationToken);

        // 3. Resolve context goals
        WeeklyGoalDto? weeklyGoal = null;
        CampaignGoalDto? campaignGoal = null;

        if (center.CenterType == CenterType.MainBranch)
        {
            // Total weekly targets configured for this center
            int totalTarget = await dbContext.BloodTypeTargets
                .Where(w => w.DonationCenterId == request.CenterId)
                .SumAsync(w => w.TargetCount, cancellationToken);

            // local week date boundaries (Saturday -> Friday)
            var localNow = dateTimeProvider.LocalNow;
            var todayDate = localNow.Date;
            int daysSinceSaturday = ((int)localNow.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
            var startOfWeek = todayDate.AddDays(-daysSinceSaturday);
            var endOfWeek = startOfWeek.AddDays(6);

            // Total completed or approved donations in this week
            int totalDonationsThisWeek = await dbContext.DonationAppointments
                .Where(da => da.DonationCenterId == request.CenterId &&
                             (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                             da.ScheduledDate >= startOfWeek &&
                             da.ScheduledDate <= endOfWeek)
                .CountAsync(cancellationToken);

            double progressPercent = totalTarget > 0
                ? Math.Min(100.0, Math.Round((double)totalDonationsThisWeek / totalTarget * 100, 1))
                : 0.0;

            weeklyGoal = new WeeklyGoalDto(progressPercent, totalDonationsThisWeek, totalTarget);
        }
        else if (center.CenterType == CenterType.Campaign)
        {
            int targetDonors = center.TargetDonors ?? 0;

            // Total registered donors (active non-cancelled bookings)
            int registeredDonors = await dbContext.DonationAppointments
                .Where(da => da.DonationCenterId == request.CenterId && da.Status != AppointmentStatus.Cancelled)
                .CountAsync(cancellationToken);

            double progressPercent = targetDonors > 0
                ? Math.Min(100.0, Math.Round((double)registeredDonors / targetDonors * 100, 1))
                : 0.0;

            campaignGoal = new CampaignGoalDto(targetDonors, registeredDonors, progressPercent);
        }

        // Parse supported donation types
        var availableDonationTypes = center.SupportedDonationTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static type => type switch
            {
                "WholeBlood" => "whole blood",
                "Platelets" => "platelets",
                "Plasma" => "plasma",
                _ => type
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var operatingHoursText = $"{center.StartTime:hh\\:mm} - {center.EndTime:hh\\:mm}";

        return new BookingDetailsDto(
            center.Id,
            center.Name,
            center.Location,
            operatingHoursText,
            averageRating,
            center.CenterType.ToString(),
            availableDonationTypes,
            weeklyGoal,
            campaignGoal);
    }
}
