using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetAppointmentStats;

public sealed class GetAppointmentStatsQueryHandler(IApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetAppointmentStatsQuery, Result<AppointmentStatsDto>>
{
    public async Task<Result<AppointmentStatsDto>> Handle(GetAppointmentStatsQuery request, CancellationToken cancellationToken)
    {
        Guid centerId;
        if (request.CenterId.HasValue)
        {
            centerId = request.CenterId.Value;
        }
        else
        {
            var mainBranch = await dbContext.DonationCenters
                .FirstOrDefaultAsync(c => c.CenterType == CenterType.MainBranch, cancellationToken);
            if (mainBranch == null)
            {
                return Result<AppointmentStatsDto>.Failure("Main branch donation center not found.");
            }
            centerId = mainBranch.Id;
        }

        var center = await dbContext.DonationCenters
            .Include(c => c.OpeningHours)
            .Include(c => c.CenterExclusions)
            .FirstOrDefaultAsync(c => c.Id == centerId, cancellationToken);

        if (center == null)
        {
            return Result<AppointmentStatsDto>.Failure($"Donation center with ID {centerId} not found.");
        }

        DateTime startDate;
        DateTime endDate;

        if (request.Date.HasValue)
        {
            startDate = request.Date.Value.Date;
            endDate = request.Date.Value.Date;
        }
        else if (request.DateFrom.HasValue && request.DateTo.HasValue)
        {
            startDate = request.DateFrom.Value.Date;
            endDate = request.DateTo.Value.Date;
        }
        else
        {
            startDate = dateTimeProvider.LocalNow.Date;
            endDate = dateTimeProvider.LocalNow.Date;
        }

        // Fetch all appointments for the date range (excluding pending ones)
        var appointments = await dbContext.DonationAppointments
            .Where(a => a.DonationCenterId == centerId)
            .Where(a => a.ScheduledDate >= startDate && a.ScheduledDate <= endDate)
            .Where(a => a.Status != AppointmentStatus.Pending)
            .ToListAsync(cancellationToken);

        // Dynamic slot capacity calculation
        var totalCapacity = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (!center.IsOperatingOn(date))
            {
                continue;
            }

            var slots = center.GenerateTimeSlotsForDate(
                date, center.CenterExclusions.ToList(), center.OpeningHours.ToList());

            totalCapacity += slots.Sum(s => s.MaxPerSlot);
        }

        var booked = appointments.Count(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed);
        var completed = appointments.Count(a => a.Status == AppointmentStatus.Completed);
        var missed = appointments.Count(a => a.Status == AppointmentStatus.NoShow);
        var cancelled = appointments.Count(a => a.Status == AppointmentStatus.Cancelled);

        // Theoretical available slots = totalCapacity - active bookings (booked + completed + missed)
        var activeBookingsCount = appointments.Count(a => a.Status != AppointmentStatus.Cancelled);
        var available = Math.Max(0, totalCapacity - activeBookingsCount);

        var stats = new AppointmentStatsDto(
            Booked: booked,
            Completed: completed,
            Missed: missed,
            Cancelled: cancelled,
            Available: available,
            Total: totalCapacity
        );

        return Result<AppointmentStatsDto>.Success(stats);
    }
}
