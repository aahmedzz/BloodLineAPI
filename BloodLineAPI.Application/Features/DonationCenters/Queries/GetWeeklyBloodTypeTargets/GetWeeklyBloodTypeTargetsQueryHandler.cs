using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonationCenters.Dtos;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.DonationCenters.Queries.GetWeeklyBloodTypeTargets
{
    public sealed class GetWeeklyBloodTypeTargetsQueryHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
        : IRequestHandler<GetWeeklyBloodTypeTargetsQuery, Result<IReadOnlyList<WeeklyBloodTypeTargetDto>>>
    {
        private static readonly string[] StandardBloodTypes = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];

        public async Task<Result<IReadOnlyList<WeeklyBloodTypeTargetDto>>> Handle(
            GetWeeklyBloodTypeTargetsQuery request,
            CancellationToken cancellationToken)
        {
            // 1. Verify center exists
            var centerExists = await dbContext.DonationCenters
                .AnyAsync(c => c.Id == request.CenterId, cancellationToken);
            if (!centerExists)
            {
                return Result<IReadOnlyList<WeeklyBloodTypeTargetDto>>.Failure("Donation center was not found.");
            }

            // 2. Resolve week date boundaries (Saturday -> Friday)
            var localNow = dateTimeProvider.LocalNow;
            var todayDate = localNow.Date;
            int daysSinceSaturday = ((int)localNow.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
            var startOfWeek = todayDate.AddDays(-daysSinceSaturday);
            var endOfWeek = startOfWeek.AddDays(6);

            // 3. Fetch configured targets for this center
            var dbTargets = await dbContext.BloodTypeTargets
                .AsNoTracking()
                .Where(w => w.DonationCenterId == request.CenterId)
                .ToDictionaryAsync(w => w.BloodType, w => w.TargetCount, cancellationToken);

            // 4. Fetch and count completed/approved donations for the current week
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

            // 5. Construct full list of 8 standard blood types
            var results = new List<WeeklyBloodTypeTargetDto>();
            foreach (var bt in StandardBloodTypes)
            {
                dbTargets.TryGetValue(bt, out int targetCount);
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
