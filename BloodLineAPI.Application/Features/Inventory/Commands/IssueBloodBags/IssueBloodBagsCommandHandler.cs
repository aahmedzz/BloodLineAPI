using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBags;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.BloodEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Inventory.Commands.IssueBloodBags;

public sealed class IssueBloodBagsCommandHandler : IRequestHandler<IssueBloodBagsCommand, IssueBloodBagsResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBackgroundNotificationService _notificationService;

    public IssueBloodBagsCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IBackgroundNotificationService notificationService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _notificationService = notificationService;
    }

    public async Task<IssueBloodBagsResult> Handle(IssueBloodBagsCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId))
            throw new UnauthorizedAccessException("Unauthenticated user.");

        var staffId = Guid.Parse(_currentUserService.UserId);
        var now = _dateTimeProvider.UtcNow;

        BloodDemand? demand = null;
        if (request.BloodDemandId.HasValue)
        {
            demand = await _dbContext.BloodDemands
                .FirstOrDefaultAsync(bd => bd.Id == request.BloodDemandId.Value, cancellationToken);

            if (demand == null)
            {
                throw new ArgumentException("طلب الدم المحدد غير موجود.");
            }

            if (demand.Status == BloodDemandStatus.Fulfilled || demand.Status == BloodDemandStatus.Cancelled)
            {
                throw new InvalidOperationException("طلب الدم المحدد مكتمل أو ملغي بالفعل.");
            }
        }

        var bags = await _dbContext.BloodBags
            .Include(bb => bb.BloodType)
            .Include(bb => bb.DonationAppointment)
                .ThenInclude(da => da!.Donor)
            .Include(bb => bb.IssuanceRecord)
                .ThenInclude(ir => ir!.IssuedByStaff)
            .Include(bb => bb.DiscardRecord)
                .ThenInclude(dr => dr!.AuthorizedByStaff)
            .Where(bb => request.BagIds.Contains(bb.Id))
            .ToListAsync(cancellationToken);

        var results = new List<BagOperationResultItem>();
        var updatedBags = new List<BloodBagDto>();
        var notifiedDonors = new List<(Guid DonorId, DateTime CollectionDate)>();
        int processed = 0;
        int failed = 0;
        int processedForDemand = 0;

        // Get staff info for the response
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

            // Validate state transition
            var (canIssue, errorCode, errorMessage) = ValidateIssueTransition(bag);
            if (!canIssue)
            {
                results.Add(new BagOperationResultItem(bagId, false, errorCode, errorMessage));
                failed++;
                continue;
            }

            // Validate blood type match for demand if applicable
            if (demand != null && bag.BloodTypeId != demand.BloodTypeId)
            {
                results.Add(new BagOperationResultItem(bagId, false, "INVALID_BLOOD_TYPE", "فصيلة دم الحقيبة لا تطابق الفصيلة المطلوبة في الطلب"));
                failed++;
                continue;
            }

            // Perform the transition
            var previousStatus = bag.Status;
            bag.Status = BloodBagStatus.Issued;

            // Create issuance record
            var issuanceRecord = new IssuanceRecord
            {
                Id = Guid.NewGuid(),
                BloodBagId = bag.Id,
                IssuedByStaffId = staffId,
                IssuedAt = now,
                RecipientName = request.RecipientName,
                NationalId = request.NationalId,
                Phone = request.Phone,
                Reason = request.Reason,
                BloodDemandId = request.BloodDemandId
            };
            await _dbContext.IssuanceRecords.AddAsync(issuanceRecord, cancellationToken);

            // Create inventory transaction
            var transaction = new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                BloodBagId = bag.Id,
                ExecutedByStaffId = staffId,
                TransactionDate = now,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = BloodBagStatus.Issued.ToString()
            };
            await _dbContext.InventoryTransactions.AddAsync(transaction, cancellationToken);

            results.Add(new BagOperationResultItem(bagId, true));
            processed++;
            if (demand != null)
            {
                processedForDemand++;
            }

            if (bag.DonationAppointment != null)
            {
                notifiedDonors.Add((bag.DonationAppointment.DonorId, bag.CollectionDate));
            }

            // Build the updated bag DTO
            var bloodType = bag.BloodType != null
                ? bag.BloodType.BloodGroupName.ToString() +
                  (bag.BloodType.RhFactor == RhFactor.Positive ? "+" : "-")
                : string.Empty;

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
                Status: "issued",
                Volume: bag.Volume,
                CreatedAt: bag.CreatedAt,
                UpdatedAt: now,
                IssuedAt: now,
                IssuedById: staffId,
                IssuedByName: staffName,
                DisposedAt: null,
                DisposedById: null,
                DisposedByName: null,
                DisposeReason: null,
                DisposeNotes: null
            ));
        }

        if (demand != null && processedForDemand > 0)
        {
            demand.IssuedUnits += processedForDemand;
            if (demand.IssuedUnits >= demand.RequestedUnits)
            {
                demand.Status = BloodDemandStatus.Fulfilled;
            }
            else
            {
                demand.Status = BloodDemandStatus.PartiallyFulfilled;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send push notifications after successful DB save
        foreach (var (donorId, collectionDate) in notifiedDonors)
        {
            try
            {
                _notificationService.EnqueueNotification(
                    donorId,
                    "تبرعك أنقذ حياة! 💖",
                    $"عزيزي المتبرع، نسعد بتبشيرك بأن تبرعك بالدم بتاريخ {collectionDate:yyyy-MM-dd} قد تم صرفه اليوم لمريض وبحاجة إليه. شكراً لكونك بطلاً ومساهماً في إنقاذ حياة إنسان! 🩸✨",
                    NotificationType.BloodBagIssued);
            }
            catch
            {
                // Ignore queue errors to keep command result success
            }
        }

        return new IssueBloodBagsResult(processed, failed, results, updatedBags);
    }

    private static (bool canIssue, string? errorCode, string? errorMessage) ValidateIssueTransition(BloodBag bag)
    {
        return bag.Status switch
        {
            BloodBagStatus.Available => (true, null, null),
            BloodBagStatus.Expired => (false, "EXPIRED_BAG", "الحقيبة منتهية الصلاحية ولا يمكن صرفها"),
            BloodBagStatus.Issued => (false, "ALREADY_ISSUED", "الحقيبة تم صرفها بالفعل"),
            BloodBagStatus.Disposed => (false, "ALREADY_DISPOSED", "الحقيبة تم إتلافها بالفعل"),
            _ => (false, "INVALID_STATUS", $"لا يمكن صرف حقيبة بحالة {bag.Status}")
        };
    }
}
