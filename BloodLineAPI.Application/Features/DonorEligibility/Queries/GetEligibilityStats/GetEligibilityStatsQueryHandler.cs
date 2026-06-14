using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Enums;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BloodLineAPI.Application.Features.DonorEligibility.Queries.GetEligibilityStats;

public sealed class GetEligibilityStatsQueryHandler(
    IApplicationDbContext dbContext,
    IOptions<DonationCooldownSettings> cooldownOptions,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetEligibilityStatsQuery, Result<EligibilityStatsDto>>
{
    public async Task<Result<EligibilityStatsDto>> Handle(
        GetEligibilityStatsQuery request,
        CancellationToken cancellationToken)
    {
        var maleDays = cooldownOptions.Value.WholeBloodMaleDays;
        var femaleDays = cooldownOptions.Value.WholeBloodFemaleDays;
        var todayLocal = dateTimeProvider.LocalNow.Date;

        // Fetch a thin projection of all donors to compute stats efficiently in memory
        var donors = await dbContext.Donors
            .Select(d => new
            {
                d.Id,
                d.Status,
                d.Gender,
                d.LastDonationDate,
                BloodType = d.BloodType != null ? d.BloodType.FullDisplayname : null
            })
            .ToListAsync(cancellationToken);

        var allCount = donors.Count;
        var eligibleCount = 0;
        var soonCount = 0;
        var notYetCount = 0;
        var deferredCount = 0;
        var ineligibleCount = 0;

        // Supported blood types
        var bloodTypeMap = new Dictionary<string, (int Eligible, int Total)>
        {
            { "A+", (0, 0) },
            { "A-", (0, 0) },
            { "B+", (0, 0) },
            { "B-", (0, 0) },
            { "AB+", (0, 0) },
            { "AB-", (0, 0) },
            { "O+", (0, 0) },
            { "O-", (0, 0) }
        };

        foreach (var d in donors)
        {
            string status;

            if (d.Status == DonorStatus.Ineligible)
            {
                status = "ineligible";
                ineligibleCount++;
            }
            else if (d.Status == DonorStatus.Deferred)
            {
                status = "deferred";
                deferredCount++;
            }
            else if (d.LastDonationDate == null)
            {
                status = "eligible";
                eligibleCount++;
            }
            else
            {
                var cooldownDays = d.Gender == Gender.Male ? maleDays : femaleDays;
                var daysSince = (todayLocal - d.LastDonationDate.Value.Date).Days;
                if (daysSince >= cooldownDays)
                {
                    status = "eligible";
                    eligibleCount++;
                }
                else
                {
                    var remaining = cooldownDays - daysSince;
                    if (remaining <= 14)
                    {
                        status = "soon";
                        soonCount++;
                    }
                    else
                    {
                        status = "not_yet";
                        notYetCount++;
                    }
                }
            }

            if (!string.IsNullOrEmpty(d.BloodType) && bloodTypeMap.ContainsKey(d.BloodType))
            {
                var current = bloodTypeMap[d.BloodType];
                var isEligible = (status == "eligible");
                bloodTypeMap[d.BloodType] = (
                    Eligible: current.Eligible + (isEligible ? 1 : 0),
                    Total: current.Total + 1
                );
            }
        }

        var statusCountsDto = new EligibilityStatusCountsDto(
            All: allCount,
            Eligible: eligibleCount,
            Soon: soonCount,
            NotYet: notYetCount,
            Deferred: deferredCount,
            Ineligible: ineligibleCount
        );

        var bloodTypeCountsDto = bloodTypeMap.ToDictionary(
            kvp => kvp.Key,
            kvp => new BloodTypeCountDto(kvp.Value.Eligible, kvp.Value.Total)
        );

        var statsDto = new EligibilityStatsDto(statusCountsDto, bloodTypeCountsDto);
        return Result<EligibilityStatsDto>.Success(statsDto);
    }
}
