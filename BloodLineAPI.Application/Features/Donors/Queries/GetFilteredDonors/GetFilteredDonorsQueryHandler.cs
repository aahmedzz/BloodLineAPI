using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using BloodLineAPI.Domain.Entities.BloodEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donors.Queries.GetFilteredDonors;

public sealed class GetFilteredDonorsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetFilteredDonorsQuery, Result<PaginatedDonorResult>>
{
    public async Task<Result<PaginatedDonorResult>> Handle(
        GetFilteredDonorsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Donors
            .Include(d => d.BloodType)
            .Include(d => d.User)
            .AsQueryable();

        // 1. Search filter (Name, Phone, NationalId)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(d =>
                d.FirstName.Contains(search) ||
                d.SecondName.Contains(search) ||
                d.ThirdName.Contains(search) ||
                (d.FourthName != null && d.FourthName.Contains(search)) ||
                (d.FirstName + " " + d.SecondName + " " + d.ThirdName + " " + (d.FourthName ?? "")).Contains(search) ||
                d.PhoneNumber.Contains(search) ||
                d.NationalId.Contains(search));
        }

        // 2. Blood Type filter (e.g. "A+", "O-")
        if (!string.IsNullOrWhiteSpace(request.BloodType))
        {
            var bloodTypeStr = request.BloodType.Trim().ToUpperInvariant();
            var hasSign = bloodTypeStr.EndsWith('+') || bloodTypeStr.EndsWith('-');
            if (hasSign)
            {
                var groupStr = bloodTypeStr[..^1];
                var sign = bloodTypeStr[^1];
                if (Enum.TryParse<BloodGroupName>(groupStr, true, out var groupName))
                {
                    var rhFactor = sign == '+' ? RhFactor.Positive : RhFactor.Negative;
                    query = query.Where(d => d.BloodType != null && d.BloodType.BloodGroupName == groupName && d.BloodType.RhFactor == rhFactor);
                }
            }
        }

        // 3. Status filter
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<DonorStatus>(request.Status, true, out var statusEnum))
            {
                query = query.Where(d => d.Status == statusEnum);
            }
        }

        // 4. District filter
        if (!string.IsNullOrWhiteSpace(request.District))
        {
            var dist = request.District.Trim();
            query = query.Where(d => d.District != null && d.District.Contains(dist));
        }

        // 5. Total count and pagination bounds
        var total = await query.CountAsync(cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var limit = request.Limit < 1 ? 10 : request.Limit;

        // 6. Materialize page records (without MedicalScreenings)
        var donorsList = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // 8. Map to DTOs in memory
        var mappedData = new List<GetAllDonorsDto>();

        foreach (var d in donorsList)
        {
            // Address string compilation
            var addressParts = new[] { d.Governorate, d.District, d.Area }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var fullAddress = addressParts.Any() ? string.Join(", ", addressParts) : (d.Address ?? string.Empty);

            var bloodTypeDisplay = d.BloodType?.FullDisplayname ?? string.Empty;

            mappedData.Add(new GetAllDonorsDto(
                Id: d.Id,
                DonorCode: d.DonorCode,
                Name: d.FullName,
                Address: fullAddress,
                BloodType: bloodTypeDisplay,
                LastDonationDate: d.LastDonationDate?.ToString("yyyy-MM-dd"),
                DonationsNumber: d.TotalDonationCount,
                EligibilityStatus: d.Status.ToString().ToLowerInvariant()
            ));
        }

        var result = new PaginatedDonorResult(mappedData, total, page, limit);
        return Result<PaginatedDonorResult>.Success(result);
    }
}

