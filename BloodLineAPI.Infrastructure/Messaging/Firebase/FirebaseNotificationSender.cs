using BloodLineAPI.Application.Common.Interfaces;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.Messaging.Firebase;

public sealed class FirebaseNotificationSender(
    IApplicationDbContext dbContext,
    ILogger<FirebaseNotificationSender> logger) : IPushNotificationDispatcher
{
    public async Task<bool> SendAsync(Guid donorId, string title, string message, CancellationToken ct = default)
    {
        return await SendAsync(donorId, title, message, null, ct);
    }

    public async Task<bool> SendAsync(Guid donorId, string title, string message, Dictionary<string, string>? data, CancellationToken ct = default)
    {
        if (FirebaseMessaging.DefaultInstance is null)
        {
            logger.LogWarning("Firebase is not initialized – skipping push notification for donor {DonorId}.", donorId);
            return false;
        }

        var tokens = await GetTokensByDonorIdAsync(donorId, ct);
        if (tokens.Count == 0)
        {
            logger.LogWarning("No device tokens found for donor {DonorId}", donorId);
            return false;
        }

        var fcmMessages = tokens.Select(token => new Message
        {
            Token = token,
            Notification = new Notification
            {
                Title = title,
                Body = message
            },
            Data = data
        }).ToList();

        try
        {
            var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(fcmMessages, cancellationToken: ct);
            await HandleResponseAsync(response, tokens, ct);
            return response.SuccessCount > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending Firebase notification to donor {DonorId}", donorId);
            return false;
        }
    }

    public async Task<bool> SendBatchAsync(IEnumerable<Guid> donorIds, string title, string message, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        if (FirebaseMessaging.DefaultInstance is null)
        {
            logger.LogWarning("Firebase is not initialized – skipping batch push notification.");
            return false;
        }

        var allTokens = new List<string>();
        foreach (var donorId in donorIds)
        {
            var tokens = await GetTokensByDonorIdAsync(donorId, ct);
            allTokens.AddRange(tokens);
        }

        if (allTokens.Count == 0)
        {
            logger.LogWarning("No device tokens found for batch of donors");
            return false;
        }

        var overallSuccess = false;

        // Firebase limit is 500 messages per single batch
        foreach (var chunk in allTokens.Chunk(500))
        {
            var fcmMessages = chunk.Select(token => new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = title,
                    Body = message
                },
                Data = data
            }).ToList();

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(fcmMessages, cancellationToken: ct);
                await HandleResponseAsync(response, chunk, ct);
                if (response.SuccessCount > 0) overallSuccess = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending batch Firebase notifications");
            }
        }

        return overallSuccess;
    }

    private async Task<IReadOnlyList<string>> GetTokensByDonorIdAsync(Guid donorId, CancellationToken ct)
    {
        return await dbContext.Donors
            .Where(d => d.Id == donorId)
            .SelectMany(d => d.User.DeviceTokens)
            .Select(dt => dt.Token)
            .ToListAsync(ct);
    }

    private async Task HandleResponseAsync(BatchResponse response, IReadOnlyList<string> sentTokens, CancellationToken ct)
    {
        if (response.FailureCount > 0)
        {
            var staleTokenValues = new List<string>();
            for (var i = 0; i < response.Responses.Count; i++)
            {
                if (!response.Responses[i].IsSuccess)
                {
                    var error = response.Responses[i].Exception?.MessagingErrorCode;
                    if (error == MessagingErrorCode.Unregistered || error == MessagingErrorCode.InvalidArgument)
                    {
                        staleTokenValues.Add(sentTokens[i]);
                    }
                }
            }

            if (staleTokenValues.Count > 0)
            {
                var tokensToRemove = await dbContext.DeviceTokens
                    .Where(t => staleTokenValues.Contains(t.Token))
                    .ToListAsync(ct);

                dbContext.DeviceTokens.RemoveRange(tokensToRemove);
                await dbContext.SaveChangesAsync(ct);
            }
        }
    }
}