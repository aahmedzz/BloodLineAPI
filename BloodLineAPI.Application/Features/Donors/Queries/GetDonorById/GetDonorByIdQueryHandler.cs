using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Donors.Queries.GetFilteredDonors;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donors.Queries.GetDonorById;

public sealed class GetDonorByIdQueryHandler(IApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetDonorByIdQuery, Result<FilteredDonorDto>>
{
    public async Task<Result<FilteredDonorDto>> Handle(
        GetDonorByIdQuery request,
        CancellationToken cancellationToken)
    {
        var donor = await dbContext.Donors
            .Include(d => d.BloodType)
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (donor == null)
        {
            return Result<FilteredDonorDto>.Failure("Donor not found.");
        }

        // Latest medical screening for deferred details
        var latestScreening = await dbContext.MedicalScreenings
            .Where(ms => ms.DonorId == donor.Id)
            .OrderByDescending(ms => ms.ScreeningDate)
            .FirstOrDefaultAsync(cancellationToken);

        var dto = FilteredDonorDto.MapFrom(donor, latestScreening, dateTimeProvider.CurrentLocalDate);

        return Result<FilteredDonorDto>.Success(dto);
    }
}
