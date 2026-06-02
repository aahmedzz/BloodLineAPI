using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetDonorAppointments;

public sealed class GetDonorAppointmentsQueryHandler(IApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetDonorAppointmentsQuery, IReadOnlyList<AppointmentListItemDto>>
{
    public async Task<IReadOnlyList<AppointmentListItemDto>> Handle(GetDonorAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.LocalNow;

        var query = dbContext.DonationAppointments
            .AsNoTracking()
            .Where(a => a.DonorId == request.DonorId)
            .Include(a => a.DonationCenter)
            .AsQueryable();

        if (request.UpcomingOnly)
        {
            query = query
                .Where(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Cancelled)
                .Where(a => a.Source == DonationSource.MobileApp)
                .Where(a => a.ScheduledDate > now.Date || (a.ScheduledDate == now.Date && a.StartTime > now.TimeOfDay))
                .OrderBy(a => a.ScheduledDate).ThenBy(a => a.StartTime);
        }
        else
        {
            query = query
                .Where(a => a.Status == AppointmentStatus.Completed ||
                            a.Status == AppointmentStatus.NoShow ||
                            a.Source == DonationSource.WalkIn ||
                            a.Source == DonationSource.Campaign ||
                            a.ScheduledDate < now.Date ||
                            (a.ScheduledDate == now.Date && a.StartTime <= now.TimeOfDay))
                .OrderByDescending(a => a.ScheduledDate).ThenByDescending(a => a.StartTime);
        }

        return await query
            .Select(a => new AppointmentListItemDto(
                a.Id,
                a.ScheduledDate,
                a.StartTime.ToString(),
                a.EndTime.ToString(),
                a.DonationType.ToString(),
                a.Status.ToString(),
                a.DonationCenter.Name,
                a.DonationCenter.Location))
            .ToListAsync(cancellationToken);
    }
}
