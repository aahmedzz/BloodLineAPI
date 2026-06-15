using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Lab.Queries.GetResults;

public sealed class GetResultsQueryHandler : IRequestHandler<GetResultsQuery, GetResultsResult>
{
    private readonly IApplicationDbContext _dbContext;

    public GetResultsQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<GetResultsResult> Handle(GetResultsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.BloodTestResults
            .AsNoTracking()
            .Include(r => r.BloodBag)
                .ThenInclude(bb => bb.DonationAppointment)
                    .ThenInclude(da => da!.Donor)
                        .ThenInclude(d => d.BloodType)
            .Include(r => r.BloodBag)
                .ThenInclude(bb => bb.BloodType)
            .Include(r => r.TestedByStaff)
            .Include(r => r.ConfirmedBloodType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(r =>
                r.BloodBag.SerialNumber.Contains(s) ||
                (r.BloodBag.DonationAppointment != null &&
                 r.BloodBag.DonationAppointment.Donor.FullName.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(request.BloodType))
        {
            var bt = request.BloodType.Trim();
            query = query.Where(r =>
                (r.BloodBag.BloodType != null &&
                 (r.BloodBag.BloodType.BloodGroupName.ToString() +
                  (r.BloodBag.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")) == bt) ||
                (r.BloodBag.DonationAppointment != null &&
                 r.BloodBag.DonationAppointment.Donor.BloodType != null &&
                 (r.BloodBag.DonationAppointment.Donor.BloodType.BloodGroupName.ToString() +
                  (r.BloodBag.DonationAppointment.Donor.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")) == bt));
        }

        if (!string.IsNullOrWhiteSpace(request.Outcome))
        {
            var oc = request.Outcome.Trim().ToLowerInvariant();
            if (oc == "safe") query = query.Where(r => r.IsSafe);
            else if (oc == "rejected") query = query.Where(r => !r.IsSafe);
        }

        var total = await query.CountAsync(cancellationToken);

        var raw = await query
            .OrderByDescending(r => r.TestDate)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var items = raw.Select(r =>
        {
            var donorName = r.BloodBag.DonationAppointment?.Donor.FullName ?? string.Empty;

            var bagBt = r.BloodBag.BloodType != null
                ? r.BloodBag.BloodType.BloodGroupName.ToString() +
                  (r.BloodBag.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                : string.Empty;

            var donorBt = r.BloodBag.DonationAppointment?.Donor.BloodType != null
                ? r.BloodBag.DonationAppointment.Donor.BloodType.BloodGroupName.ToString() +
                  (r.BloodBag.DonationAppointment.Donor.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                : string.Empty;

            var confirmedBt = r.ConfirmedBloodType != null
                ? r.ConfirmedBloodType.BloodGroupName.ToString() +
                  (r.ConfirmedBloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                : string.Empty;

            return new TestResultDto(
                r.Id,
                r.BloodBagId,
                r.BloodBag.SerialNumber,
                donorName,
                !string.IsNullOrEmpty(bagBt) ? bagBt : donorBt,
                confirmedBt,
                r.HepatitisCResult ?? string.Empty,
                r.HepatitisBResult ?? string.Empty,
                r.SyphilisResult ?? string.Empty,
                r.HivResult ?? string.Empty,
                r.IsSafe ? "safe" : "rejected",
                r.TestedByStaff?.FullName ?? string.Empty,
                r.TestDate,
                r.Notes
            );
        }).ToList();

        return new GetResultsResult(items, total, request.Page, request.Limit);
    }
}