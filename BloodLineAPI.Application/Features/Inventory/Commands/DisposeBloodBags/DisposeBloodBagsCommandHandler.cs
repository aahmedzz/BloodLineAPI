using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Inventory.Commands.IssueBloodBags;
using BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBags;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.BloodEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Inventory.Commands.DisposeBloodBags;

public sealed class DisposeBloodBagsCommandHandler : IRequestHandler<DisposeBloodBagsCommand, DisposeBloodBagsResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    private static readonly Dictionary<string, DiscardReason> ReasonMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["expired"] = DiscardReason.Expired,
        ["failed_screening"] = DiscardReason.FailedScreening,
        ["damaged_storage"] = DiscardReason.DamagedStorage,
        ["contaminated"] = DiscardReason.Contaminated,
        ["preparation_error"] = DiscardReason.PreparationError,
        ["other"] = DiscardReason.Other
    };

    public DisposeBloodBagsCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DisposeBloodBagsResult> Handle(DisposeBloodBagsCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId))
            throw new UnauthorizedAccessException("Unauthenticated user.");

        var staffId = Guid.Parse(_currentUserService.UserId);
        var now = _dateTimeProvider.UtcNow;

        if (!ReasonMap.TryGetValue(request.Reason, out var discardReason))
            throw new ArgumentException("سبب الإتلاف غير صالح");

        var bags = await _dbContext.BloodBags
            .Include(bb => bb.BloodType)
            .Include(bb => bb.DonationAppointment)
                .ThenInclude(da => da!.Donor)
            .Include(bb => bb.DiscardRecord)
                .ThenInclude(dr => dr!.AuthorizedByStaff)
            .Include(bb => bb.IssuanceRecord)
                .ThenInclude(ir => ir!.IssuedByStaff)
            .Where(bb => request.BagIds.Contains(bb.Id))
            .ToListAsync(cancellationToken);

        var results = new List<BagOperationResultItem>();
        var updatedBags = new List<BloodBagDto>();
        int processed = 0;
        int failed = 0;

        var staff = await _dbContext.Staff.FindAsync(new object[] { staffId }, cancellationToken);
        var staffName = staff?.FullName ?? string.Empty;

        foreach (var bagId in request.BagIds)
        {
            var bag = bags.FirstOrDefault(b => b.Id == bagId);

            if (bag == null)
            {
                results.Add(new BagOperationResultItem(bagId, false, "NOT_FOUND", "الحقيبة غير موجودة"));
                failed++;
                continue;
            }

            var (canDispose, errorCode, errorMessage) = ValidateDisposeTransition(bag);
            if (!canDispose)
            {
                results.Add(new BagOperationResultItem(bagId, false, errorCode, errorMessage));
                failed++;
                continue;
            }

            var previousStatus = bag.Status;
            bag.Status = BloodBagStatus.Disposed;

            // Create discard record
            var discardRecord = new DiscardRecord
            {
                Id = Guid.NewGuid(),
                BloodBagId = bag.Id,
                AuthorizedByStaffId = staffId,
                ReasonCategory = discardReason,
                ReasonDetails = request.Notes,
                DiscardDate = now
            };
            await _dbContext.DiscardRecords.AddAsync(discardRecord, cancellationToken);

            // Create inventory transaction
            var transaction = new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                BloodBagId = bag.Id,
                ExecutedByStaffId = staffId,
                TransactionDate = now,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = BloodBagStatus.Disposed.ToString()
            };
            await _dbContext.InventoryTransactions.AddAsync(transaction, cancellationToken);

            results.Add(new BagOperationResultItem(bagId, true));
            processed++;

            var bloodType = bag.BloodType != null
                ? bag.BloodType.BloodGroupName.ToString() +
                  (bag.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                : string.Empty;

            var disposeReasonString = request.Reason.ToLowerInvariant();

            updatedBags.Add(new BloodBagDto(
                Id: bag.Id,
                BagCode: bag.SerialNumber,
                BloodType: bloodType,
                DonationType: bag.BagType switch
                {
                    DonationType.WholeBlood => "wholeblood",
                    DonationType.Plasma => "plasma",
                    DonationType.Platelets => "platelets",
                    _ => bag.BagType.ToString().ToLowerInvariant()
                },
                DonorCode: bag.DonationAppointment?.Donor?.DonorCode,
                CollectedDate: bag.CollectionDate.ToString("yyyy-MM-dd"),
                ExpiryDate: bag.ExpiryDate.ToString("yyyy-MM-dd"),
                Status: "disposed",
                Volume: bag.Volume,
                CreatedAt: bag.CreatedAt,
                UpdatedAt: now,
                IssuedAt: null,
                IssuedById: null,
                IssuedByName: null,
                DisposedAt: now,
                DisposedById: staffId,
                DisposedByName: staffName,
                DisposeReason: disposeReasonString,
                DisposeNotes: request.Notes
            ));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DisposeBloodBagsResult(processed, failed, results, updatedBags);
    }

    private static (bool canDispose, string? errorCode, string? errorMessage) ValidateDisposeTransition(BloodBag bag)
    {
        return bag.Status switch
        {
            BloodBagStatus.Available => (true, null, null),
            BloodBagStatus.Expired => (true, null, null),
            BloodBagStatus.Issued => (false, "ALREADY_ISSUED", "الحقيبة تم صرفها ولا يمكن إتلافها"),
            BloodBagStatus.Disposed => (false, "ALREADY_DISPOSED", "الحقيبة تم إتلافها بالفعل"),
            _ => (false, "INVALID_STATUS", $"لا يمكن إتلاف حقيبة بحالة {bag.Status}")
        };
    }
}
