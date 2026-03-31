using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donors.Queries.GetAllDonors;

public sealed class GetAllDonorsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAllDonorsQuery, IReadOnlyList<DonorDto>>
{
    public async Task<IReadOnlyList<DonorDto>> Handle(
        GetAllDonorsQuery request,
        CancellationToken cancellationToken)
    {
        return await dbContext.Donors
            .AsNoTracking()
            .Include(d => d.BloodType)
            .Select(d => new DonorDto(
                d.Id,
                d.FullName,
                d.DateOfBirth,
                d.PhoneNumber))
            .ToListAsync(cancellationToken);
    }
}
