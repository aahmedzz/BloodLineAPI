using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Lab.Queries.GetSamples;

public sealed class GetSamplesQueryHandler : IRequestHandler<GetSamplesQuery, GetSamplesResult>
{
    private readonly IApplicationDbContext _dbContext;

    public GetSamplesQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<GetSamplesResult> Handle(GetSamplesQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.BloodBags
            .AsNoTracking()
            .Include(bb => bb.DonationAppointment)
                .ThenInclude(da => da!.Donor)
                    .ThenInclude(d => d.BloodType)
            .Include(bb => bb.DonationAppointment)
                .ThenInclude(da => da!.DonationCenter)
            .Include(bb => bb.BloodTestResults)
            .Include(bb => bb.BloodType)
            .Include(bb => bb.CollectedByStaff)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(bb =>
                bb.SerialNumber.Contains(s) ||
                bb.Id.ToString().Contains(s) ||
                (bb.DonationAppointment != null &&
                 bb.DonationAppointment.Donor.FullName.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var st = request.Status.ToLowerInvariant();
            query = st switch
            {
                "pending" => query.Where(bb => !bb.BloodTestResults.Any() && bb.Status != BloodBagStatus.Testing),
                "testing" => query.Where(bb => bb.Status == BloodBagStatus.Testing && !bb.BloodTestResults.Any()),
                "completed" => query.Where(bb => bb.BloodTestResults.Any()),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(request.BloodType))
        {
            var bt = request.BloodType.Trim();
            query = query.Where(bb =>
                (bb.BloodType != null &&
                 (bb.BloodType.BloodGroupName.ToString() +
                  (bb.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")) == bt) ||
                (bb.DonationAppointment != null &&
                 bb.DonationAppointment.Donor.BloodType != null &&
                 (bb.DonationAppointment.Donor.BloodType.BloodGroupName.ToString() +
                  (bb.DonationAppointment.Donor.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")) == bt));
        }

        var total = await query.CountAsync(cancellationToken);

        var raw = await query
            .OrderByDescending(bb => bb.CollectionDate)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var items = raw.Select(bb =>
        {
            var bagBt = bb.BloodType != null
                ? bb.BloodType.BloodGroupName.ToString() +
                  (bb.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                : string.Empty;

            var donorBt = bb.DonationAppointment?.Donor.BloodType != null
                ? bb.DonationAppointment.Donor.BloodType.BloodGroupName.ToString() +
                  (bb.DonationAppointment.Donor.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                : string.Empty;

            string status;
            if (bb.BloodTestResults.Any()) status = "completed";
            else if (bb.Status == BloodBagStatus.Testing) status = "testing";
            else status = "pending";

            return new SampleDto(
                bb.Id,
                bb.SerialNumber,
                bb.DonationAppointment?.Donor.FullName ?? string.Empty,
                !string.IsNullOrEmpty(bagBt) ? bagBt : donorBt,
                bb.BagType.ToString().ToLowerInvariant(),
                bb.CollectionDate,
                status,
                bb.Status == BloodBagStatus.Testing ? bb.CollectedByStaff?.FullName : null,
                bb.DonationAppointment?.DonationCenter.Location
            );
        }).ToList();

        return new GetSamplesResult(items, total, request.Page, request.Limit);
    }
}