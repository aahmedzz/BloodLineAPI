using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetDonationCenters;

public sealed class GetDonationCentersQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetDonationCentersQuery, IReadOnlyList<DonationCenterDto>>
{
    public async Task<IReadOnlyList<DonationCenterDto>> Handle(GetDonationCentersQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.DonationCenters.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim();
            query = query.Where(c => c.Name.Contains(search) || c.Location.Contains(search));
        }

        var centers = await query
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Location,
                c.AddressDetails,
                c.Latitude,
                c.Longitude,
                CenterType = c.CenterType.ToString(),
                Status = c.Status.ToString(),
                OperatingHours = $"{c.StartTime:hh\\:mm} - {c.EndTime:hh\\:mm}",
                c.SupportedDonationTypes
            })
            .ToListAsync(cancellationToken);

        return centers
            .Select(c => new DonationCenterDto(
                c.Id,
                c.Name,
                c.Location,
                c.AddressDetails,
                c.Latitude,
                c.Longitude,
                c.CenterType,
                c.Status,
                c.OperatingHours,
                c.SupportedDonationTypes
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(static type => type switch
                    {
                        "WholeBlood" => "whole blood",
                        "Platelets" => "platelets",
                        "Plasma" => "plasma",
                        _ => type
                    })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .ToList();
    }
}
