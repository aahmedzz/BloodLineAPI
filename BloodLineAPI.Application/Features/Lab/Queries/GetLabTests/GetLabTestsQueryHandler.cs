using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Lab.Queries.GetLabTests;

public sealed class GetLabTestsQueryHandler : IRequestHandler<GetLabTestsQuery, GetLabTestsResult>
{
    private readonly IApplicationDbContext _dbContext;

    public GetLabTestsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetLabTestsResult> Handle(GetLabTestsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.DonationAppointments
            .AsNoTracking()
            .Where(d => d.BloodBag != null)
            .Include(d => d.Donor)
                .ThenInclude(donor => donor.BloodType)
            .Include(d => d.BloodBag)
                .ThenInclude(bb => bb!.BloodTestResults)
                    .ThenInclude(r => r.TestedByStaff)
            .Include(d => d.BloodBag)
                .ThenInclude(bb => bb!.BloodTestResults)
                    .ThenInclude(r => r.ConfirmedBloodType)
            .Include(d => d.DonationCenter)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (request.Status == "pending")
                query = query.Where(d => !d.BloodBag!.BloodTestResults.Any());
            else if (request.Status == "completed")
                query = query.Where(d => d.BloodBag!.BloodTestResults.Any());
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(d =>
                (d.Donor.FirstName + " " + d.Donor.SecondName + " " + d.Donor.ThirdName + " " + (d.Donor.FourthName ?? "")).Contains(s) ||
                d.BloodBag!.SerialNumber.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(request.BloodType))
        {
            var bt = request.BloodType.Trim();
            query = query.Where(d =>
                d.Donor.BloodType != null &&
                (d.Donor.BloodType.BloodGroupName.ToString() +
                 (d.Donor.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")) == bt);
        }

        var total = await query.CountAsync(cancellationToken);

        var raw = await query
            .OrderByDescending(d => d.ScheduledDate)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var items = raw.Select(d =>
        {
            var donorBt = d.Donor.BloodType != null
                ? d.Donor.BloodType.BloodGroupName.ToString() +
                  (d.Donor.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                : string.Empty;

            var testResults = d.BloodBag!.BloodTestResults;
            var latest = testResults.OrderByDescending(r => r.TestDate).FirstOrDefault();

            LabTestResultDto? result = null;
            if (latest != null)
            {
                var confirmedBt = latest.ConfirmedBloodType != null
                    ? latest.ConfirmedBloodType.BloodGroupName.ToString() +
                      (latest.ConfirmedBloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                    : string.Empty;

                result = new LabTestResultDto(
                    latest.IsSafe ? "safe" : "rejected",
                    confirmedBt,
                    latest.HepatitisCResult ?? string.Empty,
                    latest.HepatitisBResult ?? string.Empty,
                    latest.SyphilisResult ?? string.Empty,
                    latest.HivResult ?? string.Empty,
                    latest.Notes,
                    latest.TestDate,
                    latest.TestedByStaffId,
                    latest.TestedByStaff?.FullName ?? string.Empty
                );
            }

            return new LabTestDto(
                d.Id,
                d.DonorId,
                d.Donor.FullName,
                d.BloodBag!.SerialNumber,
                donorBt,
                d.DonationType.ToString().ToLowerInvariant(),
                d.DonationCenter.Location,
                d.ScheduledDate,
                testResults.Any() ? "completed" : "pending",
                result
            );
        }).ToList();

        return new GetLabTestsResult(items, total, request.Page, request.Limit);
    }
}