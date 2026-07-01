using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Donations.Queries.GetMobileDonationHistory;

public sealed class GetMobileDonationHistoryQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMobileDonationHistoryQuery, Result<IReadOnlyList<DonationHistoryItemDto>>>
{
    public async Task<Result<IReadOnlyList<DonationHistoryItemDto>>> Handle(
        GetMobileDonationHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.DonationAppointments
            .AsNoTracking()
            .Include(da => da.DonationCenter)
            .Include(da => da.BloodBag)
                .ThenInclude(bb => bb!.BloodTestResults)
            .Where(da => da.DonorId == request.DonorId)
            .Where(da => da.Status == AppointmentStatus.Completed || da.DonationStatus == DonationStatus.Completed)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.DonationType) && !request.DonationType.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var normalized = request.DonationType.Replace(" ", "").Trim();
            if (Enum.TryParse<DonationType>(normalized, ignoreCase: true, out var typeEnum))
            {
                query = query.Where(da => da.DonationType == typeEnum);
            }
        }

        var list = await query
            .OrderByDescending(da => da.ScheduledDate)
            .ThenByDescending(da => da.StartTime)
            .ToListAsync(cancellationToken);

        var mapped = list.Select(da =>
        {
            var hasLabResults = da.BloodBag != null && da.BloodBag.BloodTestResults.Any();
            
            var donationTypeDisplay = da.DonationType switch
            {
                DonationType.WholeBlood => "Whole Blood",
                DonationType.Platelets => "Platelets",
                DonationType.Plasma => "Plasma",
                _ => da.DonationType.ToString()
            };

            return new DonationHistoryItemDto(
                da.Id,
                donationTypeDisplay,
                da.DonationCenter?.Name ?? string.Empty,
                da.ScheduledDate.ToString("yyyy-MM-dd"),
                hasLabResults
            );
        }).ToList();

        return Result<IReadOnlyList<DonationHistoryItemDto>>.Success(mapped);
    }
}
