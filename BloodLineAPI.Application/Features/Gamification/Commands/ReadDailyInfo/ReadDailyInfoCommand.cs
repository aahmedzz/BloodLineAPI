using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Enums;
using BloodLineAPI.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Gamification.Commands.ReadDailyInfo;

public record ReadDailyInfoCommand(Guid DonorId) : IRequest<Result<string>>;

public sealed class ReadDailyInfoCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ReadDailyInfoCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ReadDailyInfoCommand request, CancellationToken cancellationToken)
    {
        var donor = await dbContext.Donors.FindAsync([request.DonorId], cancellationToken);
        if (donor == null)
        {
            return Result<string>.Failure("Donor not found.");
        }

        var localNow = dateTimeProvider.LocalNow;
        var localTodayDate = localNow.Date;

        var alreadyRead = await dbContext.PointTransactions
            .AnyAsync(pt => pt.DonorId == request.DonorId &&
                            pt.ActionType == PointActionType.ReadDailyInfo &&
                            pt.TransactionDate.Date == localTodayDate,
                      cancellationToken);

        if (alreadyRead)
        {
            return Result<string>.Failure("Daily information has already been read today.");
        }

        donor.AddDomainEvent(new DailyInfoReadEvent(donor.Id, dateTimeProvider.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<string>.Success("Daily information marked as read successfully.");
    }
}
