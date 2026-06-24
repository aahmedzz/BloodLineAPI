using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donors.Commands.UpdateDonorLocation;

public sealed class UpdateDonorLocationCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateDonorLocationCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateDonorLocationCommand request, CancellationToken cancellationToken)
    {
        var donor = await dbContext.Donors
            .FirstOrDefaultAsync(d => d.Id == request.UserId, cancellationToken);

        if (donor == null)
        {
            return Result<string>.Failure("Donor profile not found.");
        }

        donor.Latitude = request.Latitude;
        donor.Longitude = request.Longitude;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<string>.Success("Location updated successfully.");
    }
}
