using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using BloodLineAPI.Domain.Events;
using BloodLineAPI.Domain.Entities.BloodEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;

using BloodLineAPI.Application.Features.Donors.Queries.GetFilteredDonors;

namespace BloodLineAPI.Application.Features.Donors.Commands.UpdateDonor;

public sealed class UpdateDonorCommandHandler(IApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<UpdateDonorCommand, Result<FilteredDonorDto>>
{
    public async Task<Result<FilteredDonorDto>> Handle(
        UpdateDonorCommand request,
        CancellationToken cancellationToken)
    {
        var donor = await dbContext.Donors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (donor == null)
        {
            return Result<FilteredDonorDto>.Failure("Donor not found.");
        }

        // 1. Update Names
        if (request.Name != null)
        {
            var nameParts = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (nameParts.Length < 3)
            {
                return Result<FilteredDonorDto>.Failure("Full name must include at least 3 names.");
            }
            donor.FirstName = nameParts[0];
            donor.SecondName = nameParts[1];
            donor.ThirdName = nameParts[2];
            donor.FourthName = nameParts.Length > 3 ? nameParts[3] : null;
        }

        // 2. Update Phone
        if (request.Phone != null)
        {
            donor.PhoneNumber = request.Phone.Trim();
            if (donor.User != null)
            {
                donor.User.PhoneNumber = request.Phone.Trim();
            }
        }

        // 3. Update Address / Governorate / District / Area
        bool addressChanged = false;
        if (request.Governorate != null)
        {
            donor.Governorate = request.Governorate.Trim();
            addressChanged = true;
        }
        if (request.District != null)
        {
            donor.District = request.District.Trim();
            addressChanged = true;
        }
        if (request.Area != null)
        {
            donor.Area = string.IsNullOrWhiteSpace(request.Area) ? null : request.Area.Trim();
            addressChanged = true;
        }
        
        if (addressChanged)
        {
            // Clear full address text or build it dynamically
            var addressParts = new[] { donor.Governorate, donor.District, donor.Area }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            donor.Address = string.Join(", ", addressParts);
        }

        // 4. Update Blood Type
        if (request.BloodType != null)
        {
            var bloodTypeStr = request.BloodType.Trim().ToUpperInvariant();
            if (bloodTypeStr.Length >= 2)
            {
                var groupStr = bloodTypeStr[..^1];
                var sign = bloodTypeStr[^1];
                if (Enum.TryParse<BloodGroupName>(groupStr, true, out var groupName))
                {
                    var rhFactor = sign == '+' ? RhFactor.Positive : RhFactor.Negative;
                    var dbBloodType = await dbContext.BloodTypes
                        .FirstOrDefaultAsync(bt => bt.BloodGroupName == groupName && bt.RhFactor == rhFactor, cancellationToken);
                    if (dbBloodType != null)
                    {
                        donor.BloodTypeId = dbBloodType.Id;
                    }
                }
            }
        }

        // 5. Update NationalId
        if (request.NationalId != null)
        {
            var nationalIdClean = request.NationalId.Trim();
            if (string.IsNullOrWhiteSpace(nationalIdClean))
            {
                return Result<FilteredDonorDto>.Failure("National ID cannot be empty.");
            }

            if (nationalIdClean != donor.NationalId)
            {
                var exists = await dbContext.Donors.AnyAsync(d => d.Id != donor.Id && d.NationalId == nationalIdClean, cancellationToken);
                if (exists)
                {
                    return Result<FilteredDonorDto>.Failure("National ID is already in use by another donor.");
                }

                donor.NationalId = nationalIdClean;
                if (donor.User != null)
                {
                    donor.User.UserName = nationalIdClean;
                    donor.User.NormalizedUserName = nationalIdClean.ToUpperInvariant();
                }
            }
        }

        // 6. Update DateOfBirth
        if (request.DateOfBirth != null)
        {
            if (DateOnly.TryParse(request.DateOfBirth, out var parsedBirthDate))
            {
                donor.DateOfBirth = parsedBirthDate;
            }
            else
            {
                return Result<FilteredDonorDto>.Failure("Invalid date format for Date of Birth. Use yyyy-MM-dd.");
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var updatedDonor = await dbContext.Donors
            .Include(d => d.BloodType)
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == donor.Id, cancellationToken);

        if (updatedDonor == null)
        {
            return Result<FilteredDonorDto>.Failure("Donor not found.");
        }

        // Latest medical screening for deferred details
        var latestScreening = await dbContext.MedicalScreenings
            .Where(ms => ms.DonorId == updatedDonor.Id)
            .OrderByDescending(ms => ms.ScreeningDate)
            .FirstOrDefaultAsync(cancellationToken);

        var dto = FilteredDonorDto.MapFrom(updatedDonor, latestScreening, dateTimeProvider.CurrentLocalDate);

        return Result<FilteredDonorDto>.Success(dto);
    }
}
