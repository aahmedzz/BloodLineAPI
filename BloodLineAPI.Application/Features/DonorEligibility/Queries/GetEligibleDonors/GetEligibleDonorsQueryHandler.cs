using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BloodLineAPI.Application.Features.DonorEligibility.Queries.GetEligibleDonors;

public sealed class GetEligibleDonorsQueryHandler(
    IApplicationDbContext dbContext,
    IDynamicSettingsService dynamicSettingsService,
    IDateTimeProvider dateTimeProvider,
    IDonorEligibilityService eligibilityService)
    : IRequestHandler<GetEligibleDonorsQuery, Result<PaginatedEligibilityResult>>
{
    public async Task<Result<PaginatedEligibilityResult>> Handle(
        GetEligibleDonorsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await dynamicSettingsService.GetSettingsAsync(cancellationToken);
        var maleDays = settings.WholeBloodMaleDays;
        var femaleDays = settings.WholeBloodFemaleDays;
        var todayLocal = dateTimeProvider.LocalNow.Date;

        var query = dbContext.Donors
            .Include(d => d.BloodType)
            .Include(d => d.User)
            .AsQueryable();

        // 1. Search Filter (Name, Phone, NationalId, DonorCode)
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
                d.NationalId.Contains(search) ||
                d.DonorCode.Contains(search));
        }

        // 2. Blood Type Filter (e.g., "A+", "O-")
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

        // 2.1. District Filter
        if (!string.IsNullOrWhiteSpace(request.District))
        {
            var dist = request.District.Trim();
            query = query.Where(d => d.District != null && d.District.Contains(dist));
        }

        // 2.2. Gender Filter
        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            if (Enum.TryParse<Gender>(request.Gender, true, out var genderEnum))
            {
                query = query.Where(d => d.Gender == genderEnum);
            }
        }

        // 3. Status Filter (translates logic to database query to paginate/filter cleanly)
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusLower = request.Status.Trim().ToLowerInvariant();
            switch (statusLower)
            {
                case "ineligible":
                    query = query.Where(d => d.Status == DonorStatus.Ineligible);
                    break;

                case "deferred":
                    query = query.Where(d => d.Status == DonorStatus.Deferred);
                    break;

                case "eligible":
                    query = query.Where(d => d.Status == DonorStatus.Eligible &&
                        (d.LastDonationDate == null ||
                         d.LastDonationDate.Value.AddDays(d.Gender == Gender.Male ? maleDays : femaleDays) <= todayLocal));
                    break;

                case "soon":
                    query = query.Where(d => d.Status == DonorStatus.Eligible &&
                        d.LastDonationDate != null &&
                        d.LastDonationDate.Value.AddDays(d.Gender == Gender.Male ? maleDays : femaleDays) > todayLocal &&
                        d.LastDonationDate.Value.AddDays(d.Gender == Gender.Male ? maleDays - 14 : femaleDays - 14) <= todayLocal);
                    break;

                case "not_yet":
                    query = query.Where(d => d.Status == DonorStatus.Eligible &&
                        d.LastDonationDate != null &&
                        d.LastDonationDate.Value.AddDays(d.Gender == Gender.Male ? maleDays - 14 : femaleDays - 14) > todayLocal);
                    break;
            }
        }

        // 4. Counts and pagination bounds
        var total = await query.CountAsync(cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var limit = request.Limit < 1 ? 10 : request.Limit;
        var totalPages = (int)Math.Ceiling((double)total / limit);

        // 5. Fetch page records
        var donors = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // 6. Map to DTOs in memory using IDonorEligibilityService for correctness
        var items = new List<EligibilityDonorDto>();
        if (donors.Count > 0)
        {
            var eligibilityMap = new Dictionary<Guid, DonorEligibilityResult?>();
            foreach (var d in donors)
            {
                var checkResult = await eligibilityService.CheckEligibilityAsync(d.Id, DonationType.WholeBlood, cancellationToken);
                eligibilityMap[d.Id] = checkResult.IsSuccess ? checkResult.Data : null;
            }

            foreach (var d in donors)
            {
                var eligibility = eligibilityMap.GetValueOrDefault(d.Id);
                var computedStatus = MapEligibilityStatus(d, eligibility);
                
                var daysLeft = 0;
                if (eligibility != null)
                {
                    if (eligibility.CooldownRemainingDays.HasValue)
                        daysLeft = eligibility.CooldownRemainingDays.Value;
                    else if (eligibility.DeferredUntil.HasValue)
                        daysLeft = Math.Max(0, (int)Math.Ceiling((eligibility.DeferredUntil.Value - todayLocal).TotalDays));
                }

                var daysAgo = d.LastDonationDate.HasValue 
                    ? (todayLocal - d.LastDonationDate.Value.Date).Days 
                    : 0;

                var eligibleDate = "—";
                if (d.Status != DonorStatus.Ineligible)
                {
                    if (eligibility != null && eligibility.IsEligible)
                    {
                        eligibleDate = "الآن";
                    }
                    else if (eligibility != null && eligibility.DeferredUntil.HasValue)
                    {
                        eligibleDate = eligibility.DeferredUntil.Value.ToString("yyyy-MM-dd");
                    }
                    else if (d.LastDonationDate.HasValue)
                    {
                        var cooldownDays = d.Gender == Gender.Male ? maleDays : femaleDays;
                        eligibleDate = d.LastDonationDate.Value.Date.AddDays(cooldownDays).ToString("yyyy-MM-dd");
                    }
                }

                var addressParts = new[] { d.Governorate, d.District, d.Area }
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                var fullAddress = addressParts.Any() ? string.Join(", ", addressParts) : (d.Address ?? string.Empty);

                var eligibilityResultDto = new EligibilityResultDto(
                    Status: computedStatus,
                    DaysLeft: daysLeft,
                    DaysAgo: daysAgo,
                    EligibleDate: eligibleDate
                );

                items.Add(new EligibilityDonorDto(
                    Id: d.Id,
                    DonorCode: d.DonorCode,
                    Name: d.FullName,
                    Gender: d.Gender.ToString().ToLowerInvariant(),
                    Age: CalculateAge(d.DateOfBirth, todayLocal),
                    NationalId: d.NationalId,
                    Phone: d.PhoneNumber,
                    Address: fullAddress,
                    District: d.District ?? string.Empty,
                    Governorate: d.Governorate,
                    Area: d.Area,
                    DateOfBirth: d.DateOfBirth.ToString("yyyy-MM-dd"),
                    BloodType: d.BloodType?.FullDisplayname,
                    Status: d.Status.ToString().ToLowerInvariant(),
                    DeferredUntil: eligibility?.DeferredUntil?.ToString("yyyy-MM-dd"),
                    LastDonationDate: d.LastDonationDate?.ToString("yyyy-MM-dd"),
                    Donations: d.TotalDonationCount,
                    HasAppAccount: d.User != null && d.User.PasswordHash != null,
                    Eligibility: eligibilityResultDto
                ));
            }
        }

        var result = new PaginatedEligibilityResult(items, total, page, limit, totalPages);
        return Result<PaginatedEligibilityResult>.Success(result);
    }

    private static string MapEligibilityStatus(Donor donor, DonorEligibilityResult? eligibility)
    {
        if (donor.Status == DonorStatus.Ineligible)
            return "ineligible";

        if (eligibility != null)
        {
            if (eligibility.DeferredUntil.HasValue)
                return "deferred";

            if (eligibility.IsEligible)
                return "eligible";

            if (eligibility.CooldownRemainingDays.HasValue)
                return eligibility.CooldownRemainingDays.Value <= 14 ? "soon" : "not_yet";
        }

        return "eligible";
    }

    private static int CalculateAge(DateOnly dateOfBirth, DateTime today)
    {
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > DateOnly.FromDateTime(today).AddYears(-age)) age--;
        return age;
    }
}
