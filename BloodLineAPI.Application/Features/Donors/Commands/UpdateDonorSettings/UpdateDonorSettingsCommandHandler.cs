using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donors.Commands.UpdateDonorSettings;

public sealed class UpdateDonorSettingsCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateDonorSettingsCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateDonorSettingsCommand request, CancellationToken cancellationToken)
    {
        var donor = await dbContext.Donors
            .FirstOrDefaultAsync(d => d.Id == request.UserId, cancellationToken);

        if (donor is null)
        {
            return Result<string>.Failure("Donor profile not found.");
        }

        donor.AllowLeaderboardVisibility = request.AllowLeaderboardVisibility;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<string>.Success("Settings updated successfully.");
    }
}
