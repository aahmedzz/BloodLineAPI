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

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new DonationCenterDto(
                c.Id,
                c.Name,
                c.Location,
                c.AddressDetails,
                c.Latitude,
                c.Longitude,
                c.CenterType.ToString(),
                c.Status.ToString(),
                $"{c.StartTime:hh\\:mm} - {c.EndTime:hh\\:mm}"))
            .ToListAsync(cancellationToken);
    }
}
