using BloodBankSystem.Domain.Entities;
using BloodBankSystem.Domain.Entities.BloodEntities;
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

        var bloodTypeEntity = await dbContext.BloodTypes
            .FirstOrDefaultAsync(bt => bt.BloodGroupName == request.BloodType, cancellationToken)
            ?? throw new InvalidOperationException($"BloodType '{request.BloodType}' not found in BloodTypes.");

        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Empty,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = request.DateOfBirth,
            BloodType = bloodTypeEntity,
            PhoneNumber = request.PhoneNumber
        };

        await dbContext.Donors.AddAsync(donor, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return donor.Id;
    }
}
