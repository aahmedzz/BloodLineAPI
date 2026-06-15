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
using BloodLineAPI.Domain.Entities.DonationEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donations.Queries.GetFilteredDonations;

public sealed class GetFilteredDonationsQueryHandler(IApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetFilteredDonationsQuery, Result<PaginatedDonationResult>>
{
    public async Task<Result<PaginatedDonationResult>> Handle(
        GetFilteredDonationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.DonationAppointments
            .Include(da => da.Donor)
                .ThenInclude(d => d.BloodType)
            .Include(da => da.DonationCenter)
            .Where(da => da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved)
            .AsQueryable();

        // 1. Apply Search Filter
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(da =>
                da.DonationCode.Contains(search) ||
                da.Donor.FirstName.Contains(search) ||
                da.Donor.SecondName.Contains(search) ||
                da.Donor.ThirdName.Contains(search) ||
                (da.Donor.FourthName != null && da.Donor.FourthName.Contains(search)) ||
                (da.Donor.FirstName + " " + da.Donor.SecondName + " " + da.Donor.ThirdName + " " + (da.Donor.FourthName ?? "")).Contains(search) ||
                da.Donor.PhoneNumber.Contains(search) ||
                da.Donor.NationalId.Contains(search));
        }

        // 2. Apply BloodType Filter
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
                    query = query.Where(da => da.Donor.BloodType != null && da.Donor.BloodType.BloodGroupName == groupName && da.Donor.BloodType.RhFactor == rhFactor);
                }
            }
        }

        // 3. Apply DonationSource Filter
        if (!string.IsNullOrWhiteSpace(request.DonationSource))
        {
            var sourceStr = request.DonationSource.Trim();
            if (sourceStr.Equals("Application", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(da => da.Source == DonationSource.MobileApp);
            }
            else if (sourceStr.Equals("Campaign", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(da => da.Source == DonationSource.Campaign);
            }
            else if (sourceStr.Equals("WalkIn", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(da => da.Source == DonationSource.WalkIn);
            }
        }

        // 4. Apply DonationStatus Filter
        if (!string.IsNullOrWhiteSpace(request.DonationStatus))
        {
            var statusStr = request.DonationStatus.Trim();
            if (statusStr.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(da => !da.SentToLab);
            }
            else if (statusStr.Equals("SentToLab", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(da => da.SentToLab);
            }
        }

        // 5. Apply Date Preset or Custom Date Range Filter
        if (!string.IsNullOrWhiteSpace(request.DatePreset))
        {
            var todayLocal = dateTimeProvider.CurrentLocalDate;
            DateTime localStart = DateTime.MinValue;
            DateTime localEnd = DateTime.MinValue;
            bool validPreset = false;

            if (request.DatePreset.Equals("today", StringComparison.OrdinalIgnoreCase))
            {
                localStart = todayLocal.ToDateTime(TimeOnly.MinValue);
                localEnd = todayLocal.AddDays(1).ToDateTime(TimeOnly.MinValue);
                validPreset = true;
            }
            else if (request.DatePreset.Equals("thisWeek", StringComparison.OrdinalIgnoreCase))
            {
                int daysSinceSaturday = ((int)todayLocal.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
                var startOfWeek = todayLocal.AddDays(-daysSinceSaturday);
                localStart = startOfWeek.ToDateTime(TimeOnly.MinValue);
                localEnd = startOfWeek.AddDays(7).ToDateTime(TimeOnly.MinValue);
                validPreset = true;
            }
            else if (request.DatePreset.Equals("thisMonth", StringComparison.OrdinalIgnoreCase))
            {
                var startOfMonth = new DateOnly(todayLocal.Year, todayLocal.Month, 1);
                localStart = startOfMonth.ToDateTime(TimeOnly.MinValue);
                localEnd = startOfMonth.AddMonths(1).ToDateTime(TimeOnly.MinValue);
                validPreset = true;
            }

            if (validPreset)
            {
                query = query.Where(da => da.CreatedAt >= localStart && da.CreatedAt < localEnd);
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.FromDate) && DateOnly.TryParse(request.FromDate, out var fromDateVal))
            {
                var localFrom = fromDateVal.ToDateTime(TimeOnly.MinValue);
                query = query.Where(da => da.CreatedAt >= localFrom);
            }

            if (!string.IsNullOrWhiteSpace(request.ToDate) && DateOnly.TryParse(request.ToDate, out var toDateVal))
            {
                var localTo = toDateVal.AddDays(1).ToDateTime(TimeOnly.MinValue);
                query = query.Where(da => da.CreatedAt < localTo);
            }
        }

        // 4. Pagination bounds
        var total = await query.CountAsync(cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var limit = request.Limit < 1 ? 10 : request.Limit;

        // 5. Materialize page records
        var donationsList = await query
            .OrderByDescending(da => da.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // Fetch MedicalScreenings since da.MedicalScreening is an ignored navigation property in EF configuration
        var appointmentIds = donationsList.Select(da => da.Id).ToList();
        var medicalScreenings = await dbContext.MedicalScreenings
            .Where(ms => ms.DonationAppointmentId.HasValue && appointmentIds.Contains(ms.DonationAppointmentId.Value))
            .ToListAsync(cancellationToken);

        var screeningsMap = medicalScreenings
            .GroupBy(ms => ms.DonationAppointmentId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var da in donationsList)
        {
            if (screeningsMap.TryGetValue(da.Id, out var screening))
            {
                da.MedicalScreening = screening;
            }
        }

        // 6. Map to DTOs in memory
        var today = dateTimeProvider.CurrentLocalDate;
        var mappedData = new List<DonationListDto>();

        foreach (var da in donationsList)
        {
            var d = da.Donor;

            // Age calculation
            var age = today.Year - d.DateOfBirth.Year;
            if (d.DateOfBirth > today.AddYears(-age)) age--;

            // Address string compilation
            var addressParts = new[] { d.Governorate, d.District, d.Area }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var fullAddress = addressParts.Any() ? string.Join(", ", addressParts) : (d.Address ?? string.Empty);

            var bloodTypeDisplay = d.BloodType?.FullDisplayname ?? string.Empty;

            string[] diseases = Array.Empty<string>();
            DonationAdditionalData? additionalData = null;
            bool isAllergic = false;

            if (da.MedicalScreening != null)
            {
                isAllergic = da.MedicalScreening.IsAllergic;
                var bp = $"{da.MedicalScreening.SystolicBP:0}/{da.MedicalScreening.DiastolicBP:0}";
                additionalData = new DonationAdditionalData(
                    Weight: da.MedicalScreening.Weight,
                    BloodPressure: bp,
                    Hemoglobin: da.MedicalScreening.HemoglobinLevel
                );

                if (!string.IsNullOrEmpty(da.MedicalScreening.ChronicDiseaseDetails))
                {
                    try
                    {
                        diseases = System.Text.Json.JsonSerializer.Deserialize<string[]>(da.MedicalScreening.ChronicDiseaseDetails) ?? Array.Empty<string>();
                    }
                    catch
                    {
                        // Fallback
                    }
                }
            }

            // Source & campaign mapping logic
            string sourceValue = "walkin";
            Guid? campaignId = null;
            string? campaignName = null;

            if (da.DonationCenter != null && da.DonationCenter.CenterType == CenterType.Campaign)
            {
                campaignId = da.DonationCenter.Id;
                campaignName = da.DonationCenter.Name;
            }

            if (da.Source == DonationSource.MobileApp)
            {
                sourceValue = "mobileapp";
            }
            else if (da.Source == DonationSource.Campaign)
            {
                sourceValue = "campaign";
            }
            else
            {
                sourceValue = "walkin";
            }

            mappedData.Add(new DonationListDto(
                Id: da.Id,
                DonationCode: da.DonationCode,
                DonorId: d.Id,
                DonorCode: d.DonorCode,
                Name: d.FullName,
                Gender: d.Gender.ToString().ToLowerInvariant(),
                Age: age,
                NationalId: d.NationalId,
                Phone: d.PhoneNumber,
                Address: fullAddress,
                District: d.District ?? string.Empty,
                BloodType: bloodTypeDisplay,
                DonationType: da.DonationType.ToString().ToLowerInvariant(),
                Source: sourceValue,
                CampaignId: campaignId,
                CampaignName: campaignName,
                DonationDate: da.CreatedAt.ToString("yyyy-MM-dd"),
                SentToLab: da.SentToLab,
                Diseases: diseases,
                AdditionalData: additionalData,
                IsAllergic: isAllergic,
                Status: da.DonationStatus.ToString().ToLowerInvariant()
            ));
        }

        var result = new PaginatedDonationResult(mappedData, total, page, limit);
        return Result<PaginatedDonationResult>.Success(result);
    }
}
