using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBags;

public sealed class GetBloodBagsQueryHandler : IRequestHandler<GetBloodBagsQuery, GetBloodBagsResult>
{
    private readonly IApplicationDbContext _dbContext;

    public GetBloodBagsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetBloodBagsResult> Handle(GetBloodBagsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.BloodBags
            .AsNoTracking()
            .Include(bb => bb.BloodType)
            .Include(bb => bb.DonationAppointment)
                .ThenInclude(da => da!.Donor)
            .Include(bb => bb.DiscardRecord)
                .ThenInclude(dr => dr!.AuthorizedByStaff)
            .Include(bb => bb.IssuanceRecord)
                .ThenInclude(ir => ir!.IssuedByStaff)
            .AsQueryable();

        // Status filter: default to Available + Expired if not specified
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<BloodBagStatus>(request.Status, ignoreCase: true, out var statusEnum))
            {
                query = query.Where(bb => bb.Status == statusEnum);
            }
        }
        else
        {
            query = query.Where(bb => bb.Status == BloodBagStatus.Available || bb.Status == BloodBagStatus.Expired);
        }

        // Search filter: matches bagCode (SerialNumber) and donorCode
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(bb =>
                bb.SerialNumber.Contains(search) ||
                (bb.DonationAppointment != null &&
                 bb.DonationAppointment.Donor != null &&
                 bb.DonationAppointment.Donor.DonorCode.Contains(search)));
        }

        // Single blood type filter
        if (!string.IsNullOrWhiteSpace(request.BloodType))
        {
            var bt = request.BloodType.Trim();
            query = query.Where(bb =>
                bb.BloodType != null &&
                (bb.BloodType.BloodGroupName.ToString() +
                 (bb.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")) == bt);
        }

        // Multi blood type filter (comma-separated)
        if (!string.IsNullOrWhiteSpace(request.BloodTypes))
        {
            var bloodTypeList = request.BloodTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = query.Where(bb =>
                bb.BloodType != null &&
                bloodTypeList.Contains(
                    bb.BloodType.BloodGroupName.ToString() +
                    (bb.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")));
        }

        // Donation type filter
        if (!string.IsNullOrWhiteSpace(request.DonationType))
        {
            if (Enum.TryParse<DonationType>(request.DonationType, ignoreCase: true, out var donationType))
            {
                query = query.Where(bb => bb.BagType == donationType);
            }
        }

        // Count before sorting/paging
        var total = await query.CountAsync(cancellationToken);

        // Sorting
        var sortBy = request.SortBy?.Trim().ToLowerInvariant() ?? "createdat";
        var isDescending = string.Equals(request.SortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? false : true;

        query = sortBy switch
        {
            "bagcode" => isDescending
                ? query.OrderByDescending(bb => bb.SerialNumber)
                : query.OrderBy(bb => bb.SerialNumber),
            "collecteddate" => isDescending
                ? query.OrderByDescending(bb => bb.CollectionDate)
                : query.OrderBy(bb => bb.CollectionDate),
            "expirydate" => isDescending
                ? query.OrderByDescending(bb => bb.ExpiryDate)
                : query.OrderBy(bb => bb.ExpiryDate),
            _ => isDescending
                ? query.OrderByDescending(bb => bb.CreatedAt)
                : query.OrderBy(bb => bb.CreatedAt)
        };

        // Pagination
        var page = Math.Max(1, request.Page);
        var limit = Math.Clamp(request.Limit, 1, 100);
        var totalPages = (int)Math.Ceiling((double)total / limit);

        var bags = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var items = bags.Select(bb =>
        {
            var bloodType = bb.BloodType != null
                ? bb.BloodType.BloodGroupName.ToString() +
                  (bb.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                : string.Empty;

            var donorCode = bb.DonationAppointment?.Donor?.DonorCode;

            var statusString = bb.Status switch
            {
                BloodBagStatus.Available => "available",
                BloodBagStatus.Expired => "expired",
                BloodBagStatus.Issued => "issued",
                BloodBagStatus.Disposed => "disposed",
                BloodBagStatus.Testing => "testing",
                _ => bb.Status.ToString().ToLowerInvariant()
            };

            var donationTypeString = bb.BagType switch
            {
                DonationType.WholeBlood => "wholeblood",
                DonationType.Plasma => "plasma",
                DonationType.Platelets => "platelets",
                _ => bb.BagType.ToString().ToLowerInvariant()
            };

            // Disposal info
            string? disposeReason = null;
            if (bb.DiscardRecord != null)
            {
                disposeReason = bb.DiscardRecord.ReasonCategory switch
                {
                    DiscardReason.Expired => "expired",
                    DiscardReason.FailedScreening => "failed_screening",
                    DiscardReason.DamagedStorage => "damaged_storage",
                    DiscardReason.Contaminated => "contaminated",
                    DiscardReason.PreparationError => "preparation_error",
                    DiscardReason.Other => "other",
                    _ => bb.DiscardRecord.ReasonCategory.ToString().ToLowerInvariant()
                };
            }

            return new BloodBagDto(
                Id: bb.Id,
                BagCode: bb.SerialNumber,
                BloodType: bloodType,
                DonationType: donationTypeString,
                DonorCode: donorCode,
                CollectedDate: bb.CollectionDate.ToString("yyyy-MM-dd"),
                ExpiryDate: bb.ExpiryDate.ToString("yyyy-MM-dd"),
                Status: statusString,
                Volume: bb.Volume,
                CreatedAt: bb.CreatedAt,
                UpdatedAt: bb.LastModifiedAt,
                IssuedAt: bb.IssuanceRecord?.IssuedAt,
                IssuedById: bb.IssuanceRecord?.IssuedByStaffId,
                IssuedByName: bb.IssuanceRecord?.IssuedByStaff?.FullName,
                DisposedAt: bb.DiscardRecord?.DiscardDate,
                DisposedById: bb.DiscardRecord?.AuthorizedByStaffId,
                DisposedByName: bb.DiscardRecord?.AuthorizedByStaff?.FullName,
                DisposeReason: disposeReason,
                DisposeNotes: bb.DiscardRecord?.ReasonDetails
            );
        }).ToList();

        return new GetBloodBagsResult(
            Items: items,
            Page: page,
            Limit: limit,
            Total: total,
            TotalPages: totalPages,
            HasNextPage: page < totalPages,
            HasPreviousPage: page > 1
        );
    }
}
