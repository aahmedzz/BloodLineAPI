using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Notifications.Commands;

public sealed record RegisterDeviceTokenCommand(
    Guid UserId, string Token, string Platform) : IRequest;

public sealed class RegisterDeviceTokenCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<RegisterDeviceTokenCommand>
{
    public async Task Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await dbContext.DeviceTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

        if (existingToken != null)
        {
            if (existingToken.UserId != request.UserId)
            {
                // Token belongs to another user now, reassign it
                existingToken.UserId = request.UserId;
                existingToken.Platform = request.Platform;
                existingToken.RegisteredAt = DateTime.UtcNow;
            }

            existingToken.LastUsedAt = DateTime.UtcNow;
        }
        else
        {
            dbContext.DeviceTokens.Add(new DeviceToken
            {
                UserId = request.UserId,
                Token = request.Token,
                Platform = request.Platform,
                RegisteredAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}