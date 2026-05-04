using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Notifications.Commands;

public sealed record UnregisterDeviceTokenCommand(
    Guid UserId, string Token) : IRequest;

public sealed class UnregisterDeviceTokenCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UnregisterDeviceTokenCommand>
{
    public async Task Handle(UnregisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.DeviceTokens
            .Where(t => t.UserId == request.UserId && t.Token == request.Token)
            .ToListAsync(cancellationToken);

        if (tokens.Count > 0)
        {
            dbContext.DeviceTokens.RemoveRange(tokens);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}