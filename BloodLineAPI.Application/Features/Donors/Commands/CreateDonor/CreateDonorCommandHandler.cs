using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using MediatR;

namespace BloodLineAPI.Application.Features.Donors.Commands.CreateDonor;

public sealed class CreateDonorCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateDonorCommand, Guid>
{
    public async Task<Guid> Handle(CreateDonorCommand request, CancellationToken cancellationToken)
    {
        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            BloodType = request.BloodType,
            PhoneNumber = request.PhoneNumber
        };

        await dbContext.Donors.AddAsync(donor, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return donor.Id;
    }
}
