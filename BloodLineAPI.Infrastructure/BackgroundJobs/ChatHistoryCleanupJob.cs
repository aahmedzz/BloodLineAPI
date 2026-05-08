using BloodLineAPI.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.BackgroundJobs;

public class ChatHistoryCleanupJob(
    IApplicationDbContext dbContext,
    ILogger<ChatHistoryCleanupJob> logger)
{
    /// <summary>
    /// Deletes conversations that have not received a new message in the last 30 days.
    /// Messages are cascade-deleted by the database.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        var staleConversations = await dbContext.ChatConversations
            .Where(c => c.LastMessageAt < cutoffDate)
            .ToListAsync(ct);

        if (staleConversations.Count == 0)
        {
            logger.LogInformation("Chat history cleanup: no stale conversations found.");
            return;
        }

        dbContext.ChatConversations.RemoveRange(staleConversations);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Chat history cleanup: deleted {Count} conversations older than {CutoffDate}.",
            staleConversations.Count, cutoffDate);
    }
}
