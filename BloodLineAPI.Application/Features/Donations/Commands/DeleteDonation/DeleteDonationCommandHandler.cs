using System;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Entities.DonationEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donations.Commands.DeleteDonation;

public sealed class DeleteDonationCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteDonationCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        DeleteDonationCommand request,
        CancellationToken cancellationToken)
    {
        var donation = await dbContext.DonationAppointments
            .FirstOrDefaultAsync(da => da.Id == request.DonationId, cancellationToken);

        if (donation == null)
        {
            return Result<string>.Failure("Donation not found.");
        }

        try
        {
            donation.CancelDonation();
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<string>.Success("Donation cancelled successfully.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}
