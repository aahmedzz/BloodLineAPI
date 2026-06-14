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

namespace BloodLineAPI.Application.Features.Campaigns.Queries.GetCampaignsList;

public sealed class GetCampaignsListQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCampaignsListQuery, Result<PaginatedCampaignsResult>>
{
    public async Task<Result<PaginatedCampaignsResult>> Handle(GetCampaignsListQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.DonationCenters
            .AsNoTracking()
            .Where(c => c.CenterType == CenterType.Campaign);

        // 1. Filter by City (exact match on Location)
        if (!string.IsNullOrWhiteSpace(request.City))
        {
            query = query.Where(c => c.Location == request.City.Trim());
        }

        // 2. Filter by Search text (contains in Name or Location)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search) || c.Location.ToLower().Contains(search));
        }

        // 3. Filter by Status
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusStr = request.Status.Trim().ToLowerInvariant();
            CenterStatus? mappedStatus = statusStr switch
            {
                "active" => CenterStatus.Active,
                "notactive" => CenterStatus.NotActive,
                "completed" => CenterStatus.Completed,
                _ => null
            };

            if (mappedStatus.HasValue)
            {
                query = query.Where(c => c.Status == mappedStatus.Value);
            }
            else
            {
                // Return empty page if status filter is invalid
                return Result<PaginatedCampaignsResult>.Success(new PaginatedCampaignsResult([], 0, request.Page, request.Limit));
            }
        }

        // 4. Sort (Latest date/time first)
        query = query.OrderByDescending(c => c.StartDate).ThenByDescending(c => c.StartTime);

        // 5. Total Count
        var total = await query.CountAsync(cancellationToken);

        // 6. Project and Paginate (optimises query counts to prevent N+1)
        var projectedQuery = query.Select(c => new
        {
            Campaign = c,
            RegisteredCount = dbContext.DonationAppointments.Count(a => a.DonationCenterId == c.Id && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Pending && a.Status != AppointmentStatus.NoShow),
            AppBookingsCount = dbContext.DonationAppointments.Count(a => a.DonationCenterId == c.Id && a.Source == DonationSource.MobileApp && a.Status != AppointmentStatus.Pending)
        });

        var pageResults = await projectedQuery
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var dtos = new List<CampaignDto>();
        foreach (var item in pageResults)
        {
            var c = item.Campaign;
            var recurrenceDto = c.RecurrenceEnabled ? new RecurrenceSettingsDto(
                Enabled: true,
                Type: (c.RecurrenceType ?? RecurrenceType.None).ToString().ToLowerInvariant(),
                WeekDays: c.RecurrenceWeekDays?.Split(',').Select(int.Parse).ToList(),
                EndDate: c.RecurrenceEndDate?.ToString("yyyy-MM-dd")
            ) : null;

            var dto = new CampaignDto(
                Id: c.CampaignCode ?? $"CAM-{c.CampaignNumber:D3}",
                Title: c.Name,
                City: c.Location,
                Latitude: c.Latitude,
                Longitude: c.Longitude,
                Date: c.StartDate.ToString("yyyy-MM-dd"),
                StartTime: c.StartTime.ToString(@"hh\:mm"),
                EndTime: c.EndTime.ToString(@"hh\:mm"),
                SlotDuration: c.SlotDurationMinutes ?? 15,
                SlotCapacity: c.MaxDonorsPerSlot,
                TargetDonors: c.TargetDonors ?? 0,
                RegisteredDonors: item.RegisteredCount,
                AppointmentsCount: item.AppBookingsCount,
                Status: c.Status.ToString().ToLowerInvariant(),
                CreatedBy: c.CreatedById?.ToString() ?? string.Empty,
                CreatedByName: c.CreatedByName ?? string.Empty,
                Description: c.DescriptionText ?? string.Empty,
                Recurrence: recurrenceDto,
                AvailableDonationTypes: c.GetSupportedDonationTypes()
            );

            dtos.Add(dto);
        }

        return Result<PaginatedCampaignsResult>.Success(new PaginatedCampaignsResult(
            Data: dtos,
            Total: total,
            Page: request.Page,
            Limit: request.Limit
        ));
    }
}
