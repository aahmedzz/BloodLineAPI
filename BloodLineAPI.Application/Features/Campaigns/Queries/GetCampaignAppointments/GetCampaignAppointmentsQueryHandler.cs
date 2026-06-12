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

namespace BloodLineAPI.Application.Features.Campaigns.Queries.GetCampaignAppointments;

public sealed class GetCampaignAppointmentsQueryHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetCampaignAppointmentsQuery, Result<IReadOnlyList<CampaignAppointmentSlotDto>>>
{
    public async Task<Result<IReadOnlyList<CampaignAppointmentSlotDto>>> Handle(GetCampaignAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var campaign = await dbContext.DonationCenters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CampaignCode == request.Id && c.CenterType == CenterType.Campaign, cancellationToken)
            ?? throw new NotFoundException(nameof(DonationCenter), request.Id);

        var appointments = await dbContext.DonationAppointments
            .AsNoTracking()
            .Include(a => a.Donor)
                .ThenInclude(d => d.BloodType)
            .Include(a => a.Donor)
                .ThenInclude(d => d.User)
            .Where(a => a.DonationCenterId == campaign.Id)
            .OrderBy(a => a.ScheduledDate)
                .ThenBy(a => a.StartTime)
            .ToListAsync(cancellationToken);

        var now = dateTimeProvider.LocalNow;
        var slots = new List<CampaignAppointmentSlotDto>();

        foreach (var app in appointments)
        {
            var age = now.Year - app.Donor.DateOfBirth.Year;
            if (now.Month < app.Donor.DateOfBirth.Month || (now.Month == app.Donor.DateOfBirth.Month && now.Day < app.Donor.DateOfBirth.Day))
            {
                age--;
            }

            var statusText = app.Status switch
            {
                AppointmentStatus.Completed => "completed",
                AppointmentStatus.Cancelled => "cancelled",
                AppointmentStatus.NoShow => "missed",
                _ => "booked"
            };

            slots.Add(new CampaignAppointmentSlotDto(
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
                DonationType: app.DonationType.ToString().ToLowerInvariant(),
                CampaignId: campaign.CampaignCode,
                Notes: app.CancellationReason,
                CompletedAt: app.Status == AppointmentStatus.Completed ? app.EndTime.ToString(@"hh\:mm") : null,
                CancelledAt: app.CancelledAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                CancelledBy: app.Status == AppointmentStatus.Cancelled ? "System" : null,
                CancelledByName: app.Status == AppointmentStatus.Cancelled ? "System" : null,
                CancellationReason: app.CancellationReason
            ));
        }

        return Result<IReadOnlyList<CampaignAppointmentSlotDto>>.Success(slots);
    }
}
