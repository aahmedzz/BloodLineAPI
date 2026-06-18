using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Inventory.Queries.GetOutflowHistory;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Inventory.Queries.ExportOutflowPdf;

public sealed class ExportOutflowPdfQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPdfGenerator pdfGenerator,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ExportOutflowPdfQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportOutflowPdfQuery request, CancellationToken cancellationToken)
    {
        // Get performing staff details
        string staffName = "System";
        if (!string.IsNullOrEmpty(currentUserService.UserId) && Guid.TryParse(currentUserService.UserId, out var staffId))
        {
            var staff = await dbContext.Staff.FindAsync([staffId], cancellationToken);
            if (staff != null)
            {
                staffName = staff.FullName;
            }
        }

        var issuanceQuery = dbContext.IssuanceRecords
            .AsNoTracking()
            .Include(ir => ir.BloodBag)
                .ThenInclude(bb => bb.BloodType)
            .Include(ir => ir.IssuedByStaff)
            .AsQueryable();

        var discardQuery = dbContext.DiscardRecords
            .AsNoTracking()
            .Include(dr => dr.BloodBag)
                .ThenInclude(bb => bb.BloodType)
            .Include(dr => dr.AuthorizedByStaff)
            .AsQueryable();

        // Parse PerformedById filter once
        Guid? perfId = null;
        if (!string.IsNullOrWhiteSpace(request.PerformedById) && Guid.TryParse(request.PerformedById, out var parsedId))
        {
            perfId = parsedId;
        }

        // Apply filters to Issuance query
        if (!string.IsNullOrWhiteSpace(request.BloodType))
        {
            var bt = request.BloodType.Trim();
            issuanceQuery = issuanceQuery.Where(ir =>
                ir.BloodBag.BloodType != null &&
                (ir.BloodBag.BloodType.BloodGroupName.ToString() + (ir.BloodBag.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")) == bt);
        }

        if (perfId.HasValue)
        {
            issuanceQuery = issuanceQuery.Where(ir => ir.IssuedByStaffId == perfId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            issuanceQuery = issuanceQuery.Where(ir =>
                ir.BloodBag.SerialNumber.Contains(search) ||
                ir.RecipientName.Contains(search) ||
                ir.IssuedByStaff.FirstName.Contains(search) ||
                ir.IssuedByStaff.SecondName.Contains(search) ||
                ir.IssuedByStaff.ThirdName.Contains(search) ||
                (ir.IssuedByStaff.FourthName != null && ir.IssuedByStaff.FourthName.Contains(search)));
        }

        // Apply filters to Discard query
        if (!string.IsNullOrWhiteSpace(request.BloodType))
        {
            var bt = request.BloodType.Trim();
            discardQuery = discardQuery.Where(dr =>
                dr.BloodBag.BloodType != null &&
                (dr.BloodBag.BloodType.BloodGroupName.ToString() + (dr.BloodBag.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")) == bt);
        }

        if (perfId.HasValue)
        {
            discardQuery = discardQuery.Where(dr => dr.AuthorizedByStaffId == perfId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            discardQuery = discardQuery.Where(dr =>
                dr.BloodBag.SerialNumber.Contains(search) ||
                dr.AuthorizedByStaff.FirstName.Contains(search) ||
                dr.AuthorizedByStaff.SecondName.Contains(search) ||
                dr.AuthorizedByStaff.ThirdName.Contains(search) ||
                (dr.AuthorizedByStaff.FourthName != null && dr.AuthorizedByStaff.FourthName.Contains(search)));
        }

        // Fetch Issuance Items (Unpaginated)
        List<OutflowUnionModel> issuanceItems = [];
        if (request.ActionType?.ToLowerInvariant() != "disposed")
        {
            var dbIssuance = await issuanceQuery
                .OrderByDescending(ir => ir.IssuedAt)
                .ToListAsync(cancellationToken);

            issuanceItems = [.. dbIssuance.Select(ir => new OutflowUnionModel
            {
                Id = ir.Id,
                BagId = ir.BloodBagId,
                BagCode = ir.BloodBag.SerialNumber,
                BloodType = ir.BloodBag.BloodType != null
                    ? ir.BloodBag.BloodType.BloodGroupName.ToString() + (ir.BloodBag.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                    : "",
                DonationType = ir.BloodBag.BagType,
                ActionType = "issued",
                RecipientName = ir.RecipientName,
                PerformedById = ir.IssuedByStaffId,
                PerformedByName = ir.IssuedByStaff.FullName,
                PerformedAt = ir.IssuedAt
            })];
        }

        // Fetch Discard Items (Unpaginated)
        List<OutflowUnionModel> discardItems = [];
        if (request.ActionType?.ToLowerInvariant() != "issued")
        {
            var dbDiscard = await discardQuery
                .OrderByDescending(dr => dr.DiscardDate)
                .ToListAsync(cancellationToken);

            discardItems = [.. dbDiscard.Select(dr => new OutflowUnionModel
            {
                Id = dr.Id,
                BagId = dr.BloodBagId,
                BagCode = dr.BloodBag.SerialNumber,
                BloodType = dr.BloodBag.BloodType != null
                    ? dr.BloodBag.BloodType.BloodGroupName.ToString() + (dr.BloodBag.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                    : "",
                DonationType = dr.BloodBag.BagType,
                ActionType = "disposed",
                RecipientName = null,
                PerformedById = dr.AuthorizedByStaffId,
                PerformedByName = dr.AuthorizedByStaff.FullName,
                PerformedAt = dr.DiscardDate
            })];
        }

        // Merge and sort in memory
        List<OutflowUnionModel> combinedItems = [.. issuanceItems.Concat(discardItems)
            .OrderByDescending(item => item.PerformedAt)];

        List<OutflowListDto> items = [];

        foreach (var item in combinedItems)
        {
            // Stable, chronological count: count of earlier records in the database before this PerformedAt
            var earlierCount = await dbContext.IssuanceRecords.CountAsync(ir => ir.IssuedAt < item.PerformedAt, cancellationToken)
                + await dbContext.DiscardRecords.CountAsync(dr => dr.DiscardDate < item.PerformedAt, cancellationToken);

            var donationTypeString = item.DonationType switch
            {
                DonationType.WholeBlood => "wholeblood",
                DonationType.Plasma => "plasma",
                DonationType.Platelets => "platelets",
                _ => item.DonationType.ToString().ToLowerInvariant()
            };

            items.Add(new OutflowListDto(
                Id: item.Id.ToString(),
                RecordCode: $"OUT-{item.PerformedAt.Year:D4}-{(earlierCount + 1):D4}",
                BagCode: item.BagCode,
                BloodType: item.BloodType,
                DonationType: donationTypeString,
                ActionType: item.ActionType,
                RecipientName: item.RecipientName,
                PerformedByName: item.PerformedByName,
                PerformedAt: item.PerformedAt
            ));
        }

        var localNow = dateTimeProvider.LocalNow;
        return pdfGenerator.GenerateOutflowReport(items, staffName, localNow);
    }

    private class OutflowUnionModel
    {
        public Guid Id { get; set; }
        public Guid BagId { get; set; }
        public string BagCode { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public DonationType DonationType { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? RecipientName { get; set; }
        public Guid PerformedById { get; set; }
        public string PerformedByName { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
    }
}
