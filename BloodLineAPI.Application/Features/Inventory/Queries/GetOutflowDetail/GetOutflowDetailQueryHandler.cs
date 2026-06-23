using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetOutflowDetail;

public sealed class GetOutflowDetailQueryHandler : IRequestHandler<GetOutflowDetailQuery, GetOutflowDetailResult?>
{
    private readonly IApplicationDbContext _dbContext;

    public GetOutflowDetailQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetOutflowDetailResult?> Handle(GetOutflowDetailQuery request, CancellationToken cancellationToken)
    {
        // Try searching in IssuanceRecords first
        var issuance = await _dbContext.IssuanceRecords
            .AsNoTracking()
            .Include(ir => ir.BloodBag)
                .ThenInclude(bb => bb.BloodType)
            .Include(ir => ir.IssuedByStaff)
            .FirstOrDefaultAsync(ir => ir.Id == request.Id, cancellationToken);

        if (issuance != null)
        {
            var performedAt = issuance.IssuedAt;
            var earlierCount = await _dbContext.IssuanceRecords.CountAsync(ir => ir.IssuedAt < performedAt, cancellationToken)
                + await _dbContext.DiscardRecords.CountAsync(dr => dr.DiscardDate < performedAt, cancellationToken);

            var bloodType = issuance.BloodBag.BloodType != null
                ? issuance.BloodBag.BloodType.BloodGroupName.ToString() + (issuance.BloodBag.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                : string.Empty;

            var donationType = issuance.BloodBag.BagType switch
            {
                DonationType.WholeBlood => "wholeblood",
                DonationType.Plasma => "plasma",
                DonationType.Platelets => "platelets",
                _ => issuance.BloodBag.BagType.ToString().ToLowerInvariant()
            };

            return new GetOutflowDetailResult(
                Id: issuance.Id.ToString(),
                RecordCode: $"OUT-{performedAt.Year:D4}-{(earlierCount + 1):D4}",
                BagCode: issuance.BloodBag.SerialNumber,
                BloodType: bloodType,
                DonationType: donationType,
                ActionType: "issued",
                RecipientName: issuance.RecipientName,
                NationalId: issuance.NationalId,
                Phone: issuance.Phone,
                Reason: issuance.Reason,
                PerformedById: issuance.IssuedByStaffId.ToString(),
                PerformedByName: issuance.IssuedByStaff.FullName,
                PerformedAt: performedAt
            );
        }

        // Try searching in DiscardRecords next
        var discard = await _dbContext.DiscardRecords
            .AsNoTracking()
            .Include(dr => dr.BloodBag)
                .ThenInclude(bb => bb.BloodType)
            .Include(dr => dr.AuthorizedByStaff)
            .FirstOrDefaultAsync(dr => dr.Id == request.Id, cancellationToken);

        if (discard != null)
        {
            var performedAt = discard.DiscardDate;
            var earlierCount = await _dbContext.IssuanceRecords.CountAsync(ir => ir.IssuedAt < performedAt, cancellationToken)
                + await _dbContext.DiscardRecords.CountAsync(dr => dr.DiscardDate < performedAt, cancellationToken);

            var bloodType = discard.BloodBag.BloodType != null
                ? discard.BloodBag.BloodType.BloodGroupName.ToString() + (discard.BloodBag.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                : string.Empty;

            var donationType = discard.BloodBag.BagType switch
            {
                DonationType.WholeBlood => "wholeblood",
                DonationType.Plasma => "plasma",
                DonationType.Platelets => "platelets",
                _ => discard.BloodBag.BagType.ToString().ToLowerInvariant()
            };

            var arabicReason = GetArabicReason(discard.ReasonCategory, discard.ReasonDetails);

            return new GetOutflowDetailResult(
                Id: discard.Id.ToString(),
                RecordCode: $"OUT-{performedAt.Year:D4}-{(earlierCount + 1):D4}",
                BagCode: discard.BloodBag.SerialNumber,
                BloodType: bloodType,
                DonationType: donationType,
                ActionType: "disposed",
                RecipientName: null,
                NationalId: null,
                Phone: null,
                Reason: arabicReason,
                PerformedById: discard.AuthorizedByStaffId.ToString(),
                PerformedByName: discard.AuthorizedByStaff.FullName,
                PerformedAt: performedAt
            );
        }

        return null;
    }

    private static string GetArabicReason(DiscardReason reason, string? details)
    {
        var baseReason = reason switch
        {
            DiscardReason.Expired => "انتهاء الصلاحية",
            DiscardReason.FailedScreening => "فشل الفحص الطبي / المخبري",
            DiscardReason.DamagedStorage => "تلف أثناء التخزين",
            DiscardReason.Contaminated => "تلوث الحقيبة",
            DiscardReason.PreparationError => "خطأ في التحضير",
            DiscardReason.Other => "أسباب أخرى",
            _ => "أخرى"
        };

        if (!string.IsNullOrWhiteSpace(details))
        {
            return $"{baseReason} ({details})";
        }
        return baseReason;
    }
}
