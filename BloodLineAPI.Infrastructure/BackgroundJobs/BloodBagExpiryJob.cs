using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class BloodBagExpiryJob
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<BloodBagExpiryJob> _logger;

    public BloodBagExpiryJob(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<BloodBagExpiryJob> logger)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var today = _dateTimeProvider.CurrentLocalDate.ToDateTime(TimeOnly.MinValue);

        var expiredBags = await _dbContext.BloodBags
            .Where(bb => bb.Status == BloodBagStatus.Available && bb.ExpiryDate <= today)
            .ToListAsync();

        if (expiredBags.Count == 0)
        {
            _logger.LogInformation("BloodBagExpiryJob: No bags to expire.");
            return;
        }

        _logger.LogInformation("BloodBagExpiryJob: Found {Count} bags to expire.", expiredBags.Count);

        foreach (var bag in expiredBags)
        {
            var previousStatus = bag.Status;
            bag.Status = BloodBagStatus.Expired;

            var transaction = new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                BloodBagId = bag.Id,
                ExecutedByStaffId = bag.CollectedByStaffId, // System-initiated, attribute to original collector
                TransactionDate = _dateTimeProvider.UtcNow,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = BloodBagStatus.Expired.ToString()
            };

            await _dbContext.InventoryTransactions.AddAsync(transaction);
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("BloodBagExpiryJob: Expired {Count} bags successfully.", expiredBags.Count);
    }
}
