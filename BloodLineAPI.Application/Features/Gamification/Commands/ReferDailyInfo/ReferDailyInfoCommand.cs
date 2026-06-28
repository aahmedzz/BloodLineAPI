using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Enums;
using BloodLineAPI.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Commands.ReferDailyInfo;

public record ReferDailyInfoCommand(Guid ReferrerId) : IRequest<Result<bool>>;

public sealed class ReferDailyInfoCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ReferDailyInfoCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ReferDailyInfoCommand request, CancellationToken cancellationToken)
    {
        var referrer = await dbContext.Donors.FindAsync([request.ReferrerId], cancellationToken);
        if (referrer == null)
        {
            return Result<bool>.Failure("Referrer not found.");
        }

        var localNow = dateTimeProvider.LocalNow;
        var localTodayDate = localNow.Date;

        var alreadyShared = await dbContext.PointTransactions
            .AnyAsync(pt => pt.DonorId == request.ReferrerId &&
                            pt.ActionType == PointActionType.ShareDailyInfo &&
                            pt.TransactionDate.Date == localTodayDate,
                      cancellationToken);

        if (alreadyShared)
        {
            return Result<bool>.Success(false); // Already shared today, just return success without raising event
        }

        referrer.AddDomainEvent(new DailyInfoSharedEvent(referrer.Id, dateTimeProvider.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
