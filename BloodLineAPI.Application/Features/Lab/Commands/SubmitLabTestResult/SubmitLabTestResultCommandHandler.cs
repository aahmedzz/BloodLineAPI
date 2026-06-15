using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities.BloodEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Lab.Commands.SubmitLabTestResult;

public sealed class SubmitLabTestResultCommandHandler : IRequestHandler<SubmitLabTestResultCommand, SubmitLabTestResultResult>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;

    public SubmitLabTestResultCommandHandler(
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext)
    {
        _currentUserService = currentUserService;
        _dbContext = dbContext;
    }

    public async Task<SubmitLabTestResultResult> Handle(SubmitLabTestResultCommand request, CancellationToken cancellationToken)
    {
        string[] allowed = { "negative", "positive" };

        if (request.Notes?.Length > 500)
            throw new ArgumentException("Notes cannot exceed 500 characters.");
        if (!allowed.Contains(request.Hcv) || !allowed.Contains(request.Hbv) ||
            !allowed.Contains(request.Syphilis) || !allowed.Contains(request.Hiv))
        {
            throw new ArgumentException("One or more screening test results are invalid.");
        }

        if (string.IsNullOrEmpty(_currentUserService.UserId))
            throw new UnauthorizedAccessException("Unauthenticated user.");

        var donation = await _dbContext.DonationAppointments
            .Include(d => d.Donor)
            .Include(d => d.DonationCenter)
            .Include(d => d.BloodBag)
                .ThenInclude(bb => bb!.BloodTestResults)
            .FirstOrDefaultAsync(d => d.Id == request.DonationAppointmentId, cancellationToken);

        if (donation == null)
            throw new KeyNotFoundException("Lab test not found.");

        if (donation.BloodBag == null)
            throw new KeyNotFoundException(
                "Blood bag not associated with this donation.");
        var bloodBag = donation.BloodBag;

        if (bloodBag.BloodTestResults.Any())
            throw new InvalidOperationException("Test already completed for this sample.");

        var outcome = (request.Hcv == "negative" && request.Hbv == "negative" &&
                       request.Syphilis == "negative" && request.Hiv == "negative")
            ? "safe" : "rejected";

        var parsed = ParseBloodTypeString(
        request.ConfirmedBloodType);

        var bt = await _dbContext.BloodTypes
            .FirstOrDefaultAsync(
                b => b.BloodGroupName == parsed.group &&
                     b.RhFactor == parsed.rh,
                cancellationToken);

        if (bt == null)
            throw new ArgumentException(
                "Invalid confirmed blood type.");

        byte confirmedBloodTypeId = bt.Id;

        var staffId = Guid.Parse(_currentUserService.UserId!);
        var now = DateTime.UtcNow;

        var testResult = new BloodTestResult
        {
            Id = Guid.NewGuid(),
            BloodBagId = bloodBag.Id,
            TestedByStaffId = staffId,
            TestDate = now,
            IsSafe = outcome == "safe",
            TestFileUrl = null,
            HepatitisCResult = request.Hcv,
            HepatitisBResult = request.Hbv,
            HivResult = request.Hiv,
            SyphilisResult = request.Syphilis,
            ConfirmedBloodTypeId = confirmedBloodTypeId,
            Notes = request.Notes
        };

        await _dbContext.BloodTestResults.AddAsync(testResult, cancellationToken);

        var previous = bloodBag.Status;
        bloodBag.Status = outcome == "safe" ? BloodBagStatus.Available : BloodBagStatus.Discarded;

        var tx = new Domain.Entities.InventoryTransaction
        {
            Id = Guid.NewGuid(),
            BloodBagId = bloodBag.Id,
            ExecutedByStaffId = staffId,
            TransactionDate = now,
            PreviousStatus = previous.ToString(),
            NewStatus = bloodBag.Status.ToString()
        };

        await _dbContext.InventoryTransactions.AddAsync(tx, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var staff = await _dbContext.Staff.FindAsync(new object[] { staffId }, cancellationToken);
        var staffName = staff?.FullName ?? string.Empty;

        return new SubmitLabTestResultResult(donation.Id, outcome, bloodBag.Id, now, staffId, staffName);
    }

    private static (BloodGroupName group, RhFactor rh)
    ParseBloodTypeString(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new ArgumentException("Invalid blood type string.");

        s = s.Trim().ToUpperInvariant();

        if (!s.EndsWith('+') && !s.EndsWith('-'))
            throw new ArgumentException("Invalid blood type format.");

        var rh = s.EndsWith('+')
            ? RhFactor.Positive
            : RhFactor.Negative;

        var grp = s[..^1];

        var group = grp switch
        {
            "A" => BloodGroupName.A,
            "B" => BloodGroupName.B,
            "AB" => BloodGroupName.AB,
            "O" => BloodGroupName.O,
            _ => throw new ArgumentException("Invalid blood group part.")
        };

        return (group, rh);
    }
}