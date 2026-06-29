using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonationCenters.Dtos;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.DonationCenters.Commands.UpdateWeeklyBloodTypeTargets
{
    public sealed class UpdateWeeklyBloodTypeTargetsCommandHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
        : IRequestHandler<UpdateWeeklyBloodTypeTargetsCommand, Result<IReadOnlyList<WeeklyBloodTypeTargetDto>>>
    {
        private static readonly string[] StandardBloodTypes = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];

        public async Task<Result<IReadOnlyList<WeeklyBloodTypeTargetDto>>> Handle(
            UpdateWeeklyBloodTypeTargetsCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Fetch center
            var center = await dbContext.DonationCenters
                .FirstOrDefaultAsync(c => c.Id == request.CenterId, cancellationToken);
            if (center == null)
            {
                return Result<IReadOnlyList<WeeklyBloodTypeTargetDto>>.Failure("Donation center was not found.");
            }

            // 2. Fetch existing targets
            var existingTargets = await dbContext.BloodTypeTargets
                .Where(w => w.DonationCenterId == request.CenterId)
                .ToListAsync(cancellationToken);

            // 3. Upsert targets
            foreach (var reqTarget in request.Targets)
            {
                var existing = existingTargets.FirstOrDefault(w => w.BloodType == reqTarget.BloodType);
                if (existing != null)
                {
                    existing.TargetCount = reqTarget.TargetCount;
                }
                else
                {
                    var newTarget = new BloodTypeTargets
                    {
                        Id = Guid.NewGuid(),
                        DonationCenterId = request.CenterId,
                        BloodType = reqTarget.BloodType,
                        TargetCount = reqTarget.TargetCount
                    };
                    dbContext.BloodTypeTargets.Add(newTarget);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // 4. Calculate total target sum and sync to DonationCenter.TargetDonors
            var allTargets = await dbContext.BloodTypeTargets
                .Where(w => w.DonationCenterId == request.CenterId)
                .ToListAsync(cancellationToken);

            int totalTargetSum = allTargets.Sum(w => w.TargetCount);
            center.TargetDonors = totalTargetSum;

            await dbContext.SaveChangesAsync(cancellationToken);

            // 5. Calculate current week progress (Saturday -> Friday)
            var localNow = dateTimeProvider.LocalNow;
            var todayDate = localNow.Date;
            int daysSinceSaturday = ((int)localNow.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
            var startOfWeek = todayDate.AddDays(-daysSinceSaturday);
            var endOfWeek = startOfWeek.AddDays(6);

            var weeklyDonations = await dbContext.DonationAppointments
                .AsNoTracking()
                .Include(da => da.Donor)
                    .ThenInclude(d => d.BloodType)
                .Where(da => da.DonationCenterId == request.CenterId &&
                             (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                             da.ScheduledDate >= startOfWeek &&
                             da.ScheduledDate <= endOfWeek)
                .ToListAsync(cancellationToken);

            var progressMap = weeklyDonations
                .Where(da => da.Donor.BloodType != null)
                .GroupBy(da => da.Donor.BloodType!.FullDisplayname)
                .ToDictionary(g => g.Key, g => g.Count());

            var dbTargetsMap = allTargets.ToDictionary(w => w.BloodType, w => w.TargetCount);

            // 6. Construct full list of 8 standard blood types
            var results = new List<WeeklyBloodTypeTargetDto>();
            foreach (var bt in StandardBloodTypes)
            {
                dbTargetsMap.TryGetValue(bt, out int targetCount);
                progressMap.TryGetValue(bt, out int progressCount);

                results.Add(new WeeklyBloodTypeTargetDto(
                    BloodType: bt,
                    TargetCount: targetCount,
                    CurrentDonationsCount: progressCount
                ));
            }

            return Result<IReadOnlyList<WeeklyBloodTypeTargetDto>>.Success(results);
        }
    }
}
