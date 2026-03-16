using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.BloodEntities;
using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donors.Commands.CreateDonor;

public sealed class CreateDonorCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateDonorCommand, Guid>
{
    public async Task<Guid> Handle(CreateDonorCommand request, CancellationToken cancellationToken)
    {
        var nameParts = request.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : request.FullName;
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Empty,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = request.DateOfBirth,
            PhoneNumber = request.PhoneNumber
        };

        await dbContext.Donors.AddAsync(donor, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return donor.Id;
    }
}
