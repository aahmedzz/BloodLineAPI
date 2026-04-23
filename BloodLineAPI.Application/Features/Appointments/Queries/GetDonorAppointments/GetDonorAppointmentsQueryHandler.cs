using BloodLineAPI.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetDonorAppointments;

public sealed class GetDonorAppointmentsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetDonorAppointmentsQuery, IReadOnlyList<AppointmentListItemDto>>
{
    public async Task<IReadOnlyList<AppointmentListItemDto>> Handle(GetDonorAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var query = dbContext.DonationAppointments
            .AsNoTracking()
            .Where(a => a.DonorId == request.DonorId)
            .Include(a => a.DonationCenter)
            .AsQueryable();

        if (request.UpcomingOnly)
        {
            query = query
                .Where(a => a.ScheduledDate >= now.Date)
                .OrderBy(a => a.ScheduledDate).ThenBy(a => a.StartTime);
        }
        else
        {
            query = query
                .Where(a => a.ScheduledDate < now.Date)
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
