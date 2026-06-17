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
using Microsoft.Extensions.Options;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Extensions;

namespace BloodLineAPI.Application.Features.Doctor.Queries.GetDoctorDashboard;

public sealed class GetDoctorDashboardQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IOptions<DonationCooldownSettings> cooldownOptions)
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

        // 2. Resolve dates
        var utcNow = dateTimeProvider.UtcNow;
        var localNow = dateTimeProvider.LocalNow;
        var todayDate = localNow.Date;

        // 3. Gather statistics, sources, weekly chart, and listings sequentially (DbContext is not thread-safe)
        var statistics = await GetStatisticsAsync(doctorUserId, todayDate, utcNow, cooldownOptions.Value, cancellationToken);
        var sources = await GetSourcesAsync(todayDate, cancellationToken);
        var weeklyChart = await GetWeeklyChartAsync(todayDate, localNow, cancellationToken);
        var activeCampaigns = await GetActiveCampaignsAsync(cancellationToken);
        var upcomingAppointments = await GetUpcomingAppointmentsAsync(todayDate, cancellationToken);
        var recentDonations = await GetRecentDonationsAsync(cancellationToken);

        // 4. Construct and return response
        var dashboardDto = new DoctorDashboardDto(
            Statistics: statistics,
            Sources: sources,
            WeeklyChart: weeklyChart,
            ActiveCampaigns: activeCampaigns,
            UpcomingAppointments: upcomingAppointments,
            RecentDonations: recentDonations
        );

        return Result<DoctorDashboardDto>.Success(dashboardDto);
    }

    #region Helper Methods

    private async Task<DashboardStatisticsDto> GetStatisticsAsync(
        Guid doctorUserId,
        DateTime todayDate,
        DateTime utcNow,
        DonationCooldownSettings cooldownSettings,
        CancellationToken cancellationToken)
    {
        var todayDonationsCount = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) && 
                              da.ScheduledDate == todayDate, 
                        cancellationToken);

        var totalDonationsCount = await dbContext.DonationAppointments
            .CountAsync(da => da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved, 
                        cancellationToken);

        // Fetch total and eligible donors in the system (overall)
        var totalDonorsCount = await dbContext.Donors
            .CountAsync(cancellationToken);

        var eligibleDonorsCount = await dbContext.Donors
            .CountAsync(DonorExtensions.IsEligiblePredicate(todayDate, utcNow, cooldownSettings), cancellationToken);

        var myActiveCampaignsCount = await dbContext.DonationCenters
            .CountAsync(c => c.CenterType == CenterType.Campaign && 
                             c.Status == CenterStatus.Active && 
                             c.CreatedById == doctorUserId, 
                        cancellationToken);

        var myTotalCampaignsCount = await dbContext.DonationCenters
            .CountAsync(c => c.CenterType == CenterType.Campaign && 
                             c.CreatedById == doctorUserId, 
                        cancellationToken);

        // Define "My Donors" as donors who had completed or approved donations screened by this doctor
        var myDonorIdsQuery = dbContext.MedicalScreenings
            .Where(ms => ms.PerformedByStaffId == doctorUserId &&
                         ms.DonationAppointmentId != null &&
                         (ms.DonationAppointment!.DonationStatus == DonationStatus.Completed || ms.DonationAppointment!.DonationStatus == DonationStatus.Approved))
            .Select(ms => ms.DonorId)
            .Distinct();

        var myDonorsCount = await dbContext.Donors
            .Where(d => myDonorIdsQuery.Contains(d.Id))
            .CountAsync(cancellationToken);

        var myEligibleDonorsCount = await dbContext.Donors
            .Where(d => myDonorIdsQuery.Contains(d.Id))
            .CountAsync(d => d.Status != DonorStatus.Ineligible && 
                             (!d.LockoutUntil.HasValue || d.LockoutUntil <= utcNow), 
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
        DateTime todayDate,
        CancellationToken cancellationToken)
    {
        var walkinTotal = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              da.DonationCenter.CenterType != CenterType.Campaign &&
                              da.Source != DonationSource.Campaign, 
                        cancellationToken);
 
        var walkinToday = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              da.DonationCenter.CenterType != CenterType.Campaign &&
                              da.Source != DonationSource.Campaign &&
                              da.ScheduledDate == todayDate, 
                        cancellationToken);
 
        var campaignTotal = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              (da.DonationCenter.CenterType == CenterType.Campaign || da.Source == DonationSource.Campaign), 
                        cancellationToken);
 
        var campaignToday = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              (da.DonationCenter.CenterType == CenterType.Campaign || da.Source == DonationSource.Campaign) &&
                              da.ScheduledDate == todayDate, 
                        cancellationToken);

        var appTotal = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              da.Source == DonationSource.MobileApp, 
                        cancellationToken);

        var appToday = await dbContext.DonationAppointments
            .CountAsync(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                              da.Source == DonationSource.MobileApp &&
                              da.ScheduledDate == todayDate, 
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
        DateTime todayDate,
        DateTime localNow,
        CancellationToken cancellationToken)
    {
        int daysSinceSaturday = ((int)localNow.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        DateTime startOfWeek = todayDate.AddDays(-daysSinceSaturday);
        DateTime endOfWeek = startOfWeek.AddDays(6);

        var donationsByDay = await dbContext.DonationAppointments
            .Where(da => (da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved) &&
                         da.ScheduledDate >= startOfWeek && da.ScheduledDate <= endOfWeek)
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
        CancellationToken cancellationToken)
    {
        return await dbContext.DonationCenters
            .AsNoTracking()
            .Where(c => c.CenterType == CenterType.Campaign && 
                        c.Status == CenterStatus.Active)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new ActiveCampaignDto(
                c.Id,
                c.Name,
                c.Status.ToString().ToLowerInvariant(),
                dbContext.DonationAppointments.Count(a => a.DonationCenterId == c.Id && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Pending && a.Status != AppointmentStatus.NoShow),
                c.TargetDonors ?? 0
            ))
            .Take(4)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<UpcomingAppointmentDto>> GetUpcomingAppointmentsAsync(
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
        CancellationToken cancellationToken)
    {
        var recentDonationsDb = await dbContext.DonationAppointments
            .AsNoTracking()
            .Include(da => da.Donor)
                .ThenInclude(d => d.BloodType)
            .Include(da => da.DonationCenter)
            .Where(da => da.DonationStatus == DonationStatus.Completed || da.DonationStatus == DonationStatus.Approved)
            .OrderByDescending(da => da.CreatedAt)
            .Take(6)
            .ToListAsync(cancellationToken);

        return recentDonationsDb.Select(da => new RecentDonationDto(
            da.Id,
            da.Donor.BloodType?.FullDisplayname ?? string.Empty,
            da.Donor.FullName,
            da.Source switch
            {
                _ when da.DonationCenter?.CenterType == CenterType.Campaign || da.Source == DonationSource.Campaign => "campaign",
                DonationSource.MobileApp => "mobileapp",
                _ => "walkin"
            },
            da.CreatedAt.ToString("yyyy-MM-dd")
        )).ToList();
    }

    #endregion
}
