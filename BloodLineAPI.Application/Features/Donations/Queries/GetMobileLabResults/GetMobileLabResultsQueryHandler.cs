using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.BloodEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Donations.Queries.GetMobileLabResults;

public sealed class GetMobileLabResultsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMobileLabResultsQuery, Result<MobileLabResultResponse>>
{
    public async Task<Result<MobileLabResultResponse>> Handle(
        GetMobileLabResultsQuery request,
        CancellationToken cancellationToken)
    {
        var donation = await dbContext.DonationAppointments
            .Include(da => da.DonationCenter)
            .Include(da => da.BloodBag)
                .ThenInclude(bb => bb!.BloodTestResults)
            .FirstOrDefaultAsync(da => da.Id == request.DonationId, cancellationToken);

        if (donation == null)
        {
            return Result<MobileLabResultResponse>.Failure("Donation appointment not found.");
        }

        if (donation.DonorId != request.DonorId)
        {
            return Result<MobileLabResultResponse>.Failure("Unauthorized access to lab results.");
        }

        if (donation.BloodBag == null)
        {
            return Result<MobileLabResultResponse>.Failure("Lab results are not available for this donation.");
        }

        var latestTestResult = donation.BloodBag.BloodTestResults
            .OrderByDescending(r => r.TestDate)
            .FirstOrDefault();

        if (latestTestResult == null)
        {
            return Result<MobileLabResultResponse>.Failure("Lab results are not available for this donation.");
        }

        var donationTypeDisplay = donation.DonationType switch
        {
            Domain.Enums.DonationType.WholeBlood => "Whole Blood",
            Domain.Enums.DonationType.Platelets => "Platelets",
            Domain.Enums.DonationType.Plasma => "Plasma",
            _ => donation.DonationType.ToString()
        };

        var parameters = new List<LabTestParameterDto>
        {
            new("HIV", FormatResult(latestTestResult.HivResult)),
            new("Hepatitis B", FormatResult(latestTestResult.HepatitisBResult)),
            new("Hepatitis C", FormatResult(latestTestResult.HepatitisCResult)),
            new("Syphilis", FormatResult(latestTestResult.SyphilisResult))
        };

        var response = new MobileLabResultResponse(
            DonationId: donation.Id,
            DonationDate: donation.ScheduledDate.ToString("yyyy-MM-dd"),
            DonationType: donationTypeDisplay,
            DonationCenterName: donation.DonationCenter?.Name ?? string.Empty,
            IsSafe: latestTestResult.IsSafe,
            Notes: latestTestResult.Notes,
            TestResults: parameters
        );

        return Result<MobileLabResultResponse>.Success(response);
    }

    private static string FormatResult(string? result)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            return "Negative"; // default/fallback
        }

        return result.Trim().ToLowerInvariant() switch
        {
            "negative" => "Negative",
            "positive" => "Positive",
            _ => char.ToUpper(result[0]) + result[1..]
        };
    }
}
