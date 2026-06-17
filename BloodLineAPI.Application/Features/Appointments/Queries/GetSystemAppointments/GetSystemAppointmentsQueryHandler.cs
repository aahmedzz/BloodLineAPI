using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetSystemAppointments;

public sealed class GetSystemAppointmentsQueryHandler(IApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetSystemAppointmentsQuery, Result<PaginatedAppointmentsResult>>
{
    public async Task<Result<PaginatedAppointmentsResult>> Handle(GetSystemAppointmentsQuery request, CancellationToken cancellationToken)
    {
        Guid centerId;
        if (request.CenterId.HasValue)
        {
            centerId = request.CenterId.Value;
        }
        else if (request.CampaignId.HasValue)
        {
            centerId = request.CampaignId.Value;
        }
        else
        {
            var mainBranch = await dbContext.DonationCenters
                .FirstOrDefaultAsync(c => c.CenterType == CenterType.MainBranch, cancellationToken);
            if (mainBranch == null)
            {
                return Result<PaginatedAppointmentsResult>.Failure("Main branch donation center not found.");
            }
            centerId = mainBranch.Id;
        }

        var center = await dbContext.DonationCenters
            .Include(c => c.OpeningHours)
            .Include(c => c.CenterExclusions)
            .FirstOrDefaultAsync(c => c.Id == centerId, cancellationToken);

        if (center == null)
        {
            return Result<PaginatedAppointmentsResult>.Failure($"Donation center with ID {centerId} not found.");
        }

        // Determine date range
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

        // Fetch appointments matching center and date range
        var appointments = await dbContext.DonationAppointments
            .Include(a => a.Donor)
                .ThenInclude(d => d.BloodType)
            .Where(a => a.DonationCenterId == centerId)
            .Where(a => a.ScheduledDate >= startDate && a.ScheduledDate <= endDate)
            .Where(a => a.Status != AppointmentStatus.Pending)
            .Where(a => a.Source == DonationSource.MobileApp)
            .ToListAsync(cancellationToken);

        var today = dateTimeProvider.CurrentLocalDate;
        var allSlotsList = new List<SystemAppointmentSlotDto>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (!center.IsOperatingOn(date))
            {
                continue;
            }

            var timeSlots = center.GenerateTimeSlotsForDate(
                date, center.CenterExclusions.ToList(), center.OpeningHours.ToList());

            var dayAppointments = appointments
                .Where(a => a.ScheduledDate.Date == date.Date)
                .ToList();

            foreach (var slot in timeSlots)
            {
                var slotAppointments = dayAppointments
                    .Where(a => a.StartTime == slot.Start)
                    .ToList();

                // 1. Map existing booked/cancelled appointments
                foreach (var app in slotAppointments)
                {
                    var age = today.Year - app.Donor.DateOfBirth.Year;
                    if (app.Donor.DateOfBirth > today.AddYears(-age)) age--;

                    var statusText = app.Status switch
                    {
                        AppointmentStatus.Pending => "booked",
                        AppointmentStatus.Confirmed => "booked",
                        AppointmentStatus.Completed => "completed",
                        AppointmentStatus.Cancelled => "cancelled",
                        AppointmentStatus.NoShow => "noshow",
                        _ => "booked"
                    };

                    allSlotsList.Add(new SystemAppointmentSlotDto(
                        Id: app.Id.ToString(),
                        Date: app.ScheduledDate.ToString("yyyy-MM-dd"),
                        Time: app.StartTime.ToString(@"hh\:mm"),
                        Status: statusText,
                        DonorName: app.Donor.FullName,
                        DonorCode: app.Donor.DonorCode,
                        DonorNationalId: app.Donor.NationalId,
                        DonorPhone: app.Donor.PhoneNumber,
                        DonorBloodType: app.Donor.BloodType?.FullDisplayname,
                        DonorGender: app.Donor.Gender.ToString().ToLowerInvariant(),
                        DonorAge: age,
                        DonorDistrict: app.Donor.District,
                        DonorArea: app.Donor.Area,
                        DonationType: app.DonationType.ToString().ToLowerInvariant(),
                        CampaignId: center.CenterType == CenterType.Campaign ? center.Id : null,
                        Notes: app.CancellationReason,
                        CompletedAt: app.Status == AppointmentStatus.Completed ? app.EndTime.ToString(@"hh\:mm") : null,
                        CancelledAt: app.CancelledAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        CancellationReason: app.CancellationReason
                    ));
                }


            }
        }

        // Apply Status Filter in memory
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusFilter = request.Status.Trim().ToLowerInvariant();
            allSlotsList = allSlotsList
                .Where(s => s.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var total = allSlotsList.Count;
        var pagedItems = allSlotsList
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToList();

        var result = new PaginatedAppointmentsResult(pagedItems, total, request.Page, request.Limit);
        return Result<PaginatedAppointmentsResult>.Success(result);
    }
}
