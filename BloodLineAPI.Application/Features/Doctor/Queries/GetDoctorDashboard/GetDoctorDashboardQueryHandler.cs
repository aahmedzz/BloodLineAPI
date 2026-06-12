using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Doctor.Dtos;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Doctor.Queries.GetDoctorDashboard;

public sealed class GetDoctorDashboardQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetDoctorDashboardQuery, Result<DoctorDashboardDto>>
{
    public async Task<Result<DoctorDashboardDto>> Handle(GetDoctorDashboardQuery request, CancellationToken cancellationToken)
    {
        // 1. Resolve current authenticated doctor user
        var currentUserId = currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var doctorUserId))
        {
            return Result<DoctorDashboardDto>.Failure("User is not authenticated.");
        }

        // 2. Resolve the doctor's primary branch center
        var center = await GetDoctorPrimaryCenterAsync(doctorUserId, cancellationToken);

        // 3. Resolve dates
        var utcNow = dateTimeProvider.UtcNow;
        var localNow = dateTimeProvider.LocalNow;
        var todayDate = localNow.Date;

        // 4. Gather statistics, sources, weekly chart, and listings in parallel tasks for optimal performance
        var statisticsTask = GetStatisticsAsync(center, todayDate, utcNow, cancellationToken);
        var sourcesTask = GetSourcesAsync(center, todayDate, cancellationToken);
        var weeklyChartTask = GetWeeklyChartAsync(center, todayDate, localNow, cancellationToken);
        var activeCampaignsTask = GetActiveCampaignsAsync(center, cancellationToken);
        var upcomingAppointmentsTask = GetUpcomingAppointmentsAsync(center, todayDate, cancellationToken);
        var recentDonationsTask = GetRecentDonationsAsync(center, cancellationToken);

        await Task.WhenAll(
            statisticsTask,
            sourcesTask,
            weeklyChartTask,
            activeCampaignsTask,
            upcomingAppointmentsTask,
            recentDonationsTask
        );

        // 5. Construct and return response
        var dashboardDto = new DoctorDashboardDto(
            Statistics: await statisticsTask,
            Sources: await sourcesTask,
            WeeklyChart: await weeklyChartTask,
            ActiveCampaigns: await activeCampaignsTask,
            UpcomingAppointments: await upcomingAppointmentsTask,
            RecentDonations: await recentDonationsTask
        );

        return Result<DoctorDashboardDto>.Success(dashboardDto);
    }

    #region Helper Methods

    private async Task<DonationCenter?> GetDoctorPrimaryCenterAsync(Guid doctorUserId, CancellationToken cancellationToken)
    {
        var staff = await dbContext.Staff
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == doctorUserId, cancellationToken);

        DonationCenter? center = null;
        if (staff != null && !string.IsNullOrEmpty(staff.City))
        {
            var cityLower = staff.City.Trim().ToLower();
            center = await dbContext.DonationCenters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CenterType == CenterType.MainBranch && c.Location.ToLower() == cityLower, cancellationToken);
        }

        return center ?? await dbContext.DonationCenters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CenterType == CenterType.MainBranch, cancellationToken);
    }

    private async Task<DashboardStatisticsDto> GetStatisticsAsync(
        DonationCenter? center,
        DateTime todayDate,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var todayDonationsCount = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) && 
                              da.ScheduledDate == todayDate &&
                              (center == null || da.DonationCenterId == center.Id || (da.DonationCenter.CenterType == CenterType.Campaign && da.DonationCenter.Location == center.Location)), 
                        cancellationToken);

        var totalDonationsCount = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              (center == null || da.DonationCenterId == center.Id || (da.DonationCenter.CenterType == CenterType.Campaign && da.DonationCenter.Location == center.Location)), 
                        cancellationToken);

        // Fetch unique donors associated with the main branch or its campaigns
        var branchCenterIdsQuery = dbContext.DonationCenters
            .Where(c => center == null || c.Id == center.Id || (c.CenterType == CenterType.Campaign && c.Location == center.Location))
            .Select(c => c.Id);

        var branchDonorIdsQuery = dbContext.DonationAppointments
            .Where(da => branchCenterIdsQuery.Contains(da.DonationCenterId))
            .Select(da => da.DonorId)
            .Distinct();

        var totalDonorsCount = await dbContext.Donors
            .Where(d => branchDonorIdsQuery.Contains(d.Id))
            .CountAsync(cancellationToken);

        var eligibleDonorsCount = await dbContext.Donors
            .Where(d => branchDonorIdsQuery.Contains(d.Id))
            .CountAsync(d => d.Status != DonorStatus.Ineligible && 
                             !dbContext.MedicalScreenings.Any(ms => ms.DonorId == d.Id && !ms.IsEligible && ms.LockoutUntil != null && ms.LockoutUntil > utcNow), 
                        cancellationToken);

        var myActiveCampaignsCount = await dbContext.DonationCenters
            .CountAsync(c => c.CenterType == CenterType.Campaign && 
                             c.Status == CenterStatus.Active && 
                             (center == null || c.Location == center.Location), 
                        cancellationToken);

        var myTotalCampaignsCount = await dbContext.DonationCenters
            .CountAsync(c => c.CenterType == CenterType.Campaign && 
                             (center == null || c.Location == center.Location), 
                        cancellationToken);

        // Define "My Donors" as donors who booked/donated in campaigns linked to this branch/location
        var myCampaignDonorIdsQuery = dbContext.DonationAppointments
            .Where(da => da.DonationCenter.CenterType == CenterType.Campaign && 
                         (center == null || da.DonationCenter.Location == center.Location))
            .Select(da => da.DonorId)
            .Distinct();

        var myDonorsCount = await dbContext.Donors
            .Where(d => myCampaignDonorIdsQuery.Contains(d.Id))
            .CountAsync(cancellationToken);

        var myEligibleDonorsCount = await dbContext.Donors
            .Where(d => myCampaignDonorIdsQuery.Contains(d.Id))
            .CountAsync(d => d.Status != DonorStatus.Ineligible && 
                             !dbContext.MedicalScreenings.Any(ms => ms.DonorId == d.Id && !ms.IsEligible && ms.LockoutUntil != null && ms.LockoutUntil > utcNow), 
                        cancellationToken);

        return new DashboardStatisticsDto(
            TodayDonationsCount: todayDonationsCount,
            TotalDonationsCount: totalDonationsCount,
            TotalDonorsCount: totalDonorsCount,
            EligibleDonorsCount: eligibleDonorsCount,
            MyActiveCampaignsCount: myActiveCampaignsCount,
            MyTotalCampaignsCount: myTotalCampaignsCount,
            MyDonorsCount: myDonorsCount,
            MyEligibleDonorsCount: myEligibleDonorsCount
        );
    }

    private async Task<DonationSourceStatsDto> GetSourcesAsync(
        DonationCenter? center,
        DateTime todayDate,
        CancellationToken cancellationToken)
    {
        var walkinTotal = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              da.Source == DonationSource.WalkIn &&
                              (center == null || da.DonationCenterId == center.Id), 
                        cancellationToken);

        var walkinToday = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              da.Source == DonationSource.WalkIn &&
                              da.ScheduledDate == todayDate &&
                              (center == null || da.DonationCenterId == center.Id), 
                        cancellationToken);

        var campaignTotal = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              (da.Source == DonationSource.Campaign || da.DonationCenter.CenterType == CenterType.Campaign) &&
                              (center == null || da.DonationCenter.Location == center.Location), 
                        cancellationToken);

        var campaignToday = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              (da.Source == DonationSource.Campaign || da.DonationCenter.CenterType == CenterType.Campaign) &&
                              da.ScheduledDate == todayDate &&
                              (center == null || da.DonationCenter.Location == center.Location), 
                        cancellationToken);

        var appTotal = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              da.Source == DonationSource.MobileApp &&
                              (center == null || da.DonationCenterId == center.Id), 
                        cancellationToken);

        var appToday = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              da.Source == DonationSource.MobileApp &&
                              da.ScheduledDate == todayDate &&
                              (center == null || da.DonationCenterId == center.Id), 
                        cancellationToken);

        return new DonationSourceStatsDto(
            WalkinTotal: walkinTotal,
            WalkinToday: walkinToday,
            CampaignTotal: campaignTotal,
            CampaignToday: campaignToday,
            AppTotal: appTotal,
            AppToday: appToday
        );
    }

    private async Task<IReadOnlyList<WeeklyDonationChartDto>> GetWeeklyChartAsync(
        DonationCenter? center,
        DateTime todayDate,
        DateTime localNow,
        CancellationToken cancellationToken)
    {
        int daysSinceSaturday = ((int)localNow.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        DateTime startOfWeek = todayDate.AddDays(-daysSinceSaturday);
        DateTime endOfWeek = startOfWeek.AddDays(6);

        var donationsByDay = await dbContext.DonationAppointments
            .Where(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                         da.ScheduledDate >= startOfWeek && da.ScheduledDate <= endOfWeek &&
                         (center == null || da.DonationCenterId == center.Id || (da.DonationCenter.CenterType == CenterType.Campaign && da.DonationCenter.Location == center.Location)))
            .GroupBy(da => da.ScheduledDate)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var donationCountsMap = donationsByDay.ToDictionary(x => x.Date.Date, x => x.Count);

        var weeklyChart = new List<WeeklyDonationChartDto>();
        string[] dayNames = { "السبت", "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة" };

        for (int i = 0; i < 7; i++)
        {
            var dateOfSlot = startOfWeek.AddDays(i);
            var dateStr = dateOfSlot.ToString("yyyy-MM-dd");

            int donationsCount = 0;
            if (dateOfSlot <= todayDate)
            {
                donationCountsMap.TryGetValue(dateOfSlot.Date, out donationsCount);
            }

            weeklyChart.Add(new WeeklyDonationChartDto(
                DayName: dayNames[i],
                Date: dateStr,
                DonationsCount: donationsCount
            ));
        }

        return weeklyChart;
    }

    private async Task<IReadOnlyList<ActiveCampaignDto>> GetActiveCampaignsAsync(
        DonationCenter? center,
        CancellationToken cancellationToken)
    {
        return await dbContext.DonationCenters
            .AsNoTracking()
            .Where(c => c.CenterType == CenterType.Campaign && 
                        c.Status == CenterStatus.Active &&
                        (center == null || c.Location == center.Location))
            .OrderByDescending(c => c.StartDate)
            .Select(c => new ActiveCampaignDto(
                c.Id,
                c.Name,
                c.Status.ToString().ToLowerInvariant(),
                dbContext.DonationAppointments.Count(a => a.DonationCenterId == c.Id && a.Status != AppointmentStatus.Cancelled),
                c.TargetDonors ?? 0
            ))
            .Take(4)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<UpcomingAppointmentDto>> GetUpcomingAppointmentsAsync(
        DonationCenter? center,
        DateTime todayDate,
        CancellationToken cancellationToken)
    {
        return await dbContext.DonationAppointments
            .AsNoTracking()
            .Include(a => a.Donor)
                .ThenInclude(d => d.BloodType)
            .Include(a => a.DonationCenter)
            .Where(a => a.ScheduledDate == todayDate && 
                        (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
            .Where(a => center == null || a.DonationCenterId == center.Id || (a.DonationCenter.CenterType == CenterType.Campaign && a.DonationCenter.Location == center.Location))
            .OrderBy(a => a.StartTime)
            .Select(a => new UpcomingAppointmentDto(
                a.Id,
                a.StartTime.ToString(@"hh\:mm"),
                a.Donor.FullName,
                a.Donor.NationalId,
                a.Donor.BloodType != null ? a.Donor.BloodType.FullDisplayname : string.Empty,
                "booked"
            ))
            .Take(6)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<RecentDonationDto>> GetRecentDonationsAsync(
        DonationCenter? center,
        CancellationToken cancellationToken)
    {
        var recentDonationsDb = await dbContext.DonationAppointments
            .AsNoTracking()
            .Include(da => da.Donor)
                .ThenInclude(d => d.BloodType)
            .Include(da => da.DonationCenter)
            .Where(da => da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved)
            .Where(da => center == null || da.DonationCenterId == center.Id || (da.DonationCenter.CenterType == CenterType.Campaign && da.DonationCenter.Location == center.Location))
            .OrderByDescending(da => da.CreatedAt)
            .Take(6)
            .ToListAsync(cancellationToken);

        return recentDonationsDb.Select(da => new RecentDonationDto(
            da.Id,
            da.Donor.BloodType?.FullDisplayname ?? string.Empty,
            da.Donor.FullName,
            da.Source switch
            {
                DonationSource.MobileApp => "mobileapp",
                DonationSource.Campaign => "campaign",
                _ => da.DonationCenter?.CenterType == CenterType.Campaign ? "campaign" : "walkin"
            },
            da.CreatedAt.ToString("yyyy-MM-dd")
        )).ToList();
    }

    #endregion
}
