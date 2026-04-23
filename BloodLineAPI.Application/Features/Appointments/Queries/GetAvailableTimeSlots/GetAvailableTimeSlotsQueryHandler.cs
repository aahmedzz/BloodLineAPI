using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetAvailableTimeSlots;

public sealed class GetAvailableTimeSlotsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAvailableTimeSlotsQuery, IReadOnlyList<TimeSlotDto>>
{
    public async Task<IReadOnlyList<TimeSlotDto>> Handle(GetAvailableTimeSlotsQuery request, CancellationToken cancellationToken)
    {
        var center = await dbContext.DonationCenters
            .Include(c => c.OpeningHours)
            .Include(c => c.CenterExclusions)
            .FirstOrDefaultAsync(c => c.Id == request.DonationCenterId, cancellationToken)
            ?? throw new NotFoundException("DonationCenter", request.DonationCenterId);

        if (!center.IsOperatingOn(request.Date))
        {
            return [];
        }

        var slots = center.GenerateTimeSlotsForDate(request.Date, center.CenterExclusions.ToList(), center.OpeningHours.ToList());
        if (slots.Count == 0)
        {
            return [];
        }

        var bookedSlots = await dbContext.DonationAppointments
            .Where(a => a.DonationCenterId == request.DonationCenterId)
            .Where(a => a.ScheduledDate == request.Date.Date)
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .GroupBy(a => a.StartTime)
            .Select(g => new { StartTime = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StartTime, x => x.Count, cancellationToken);

        return slots.Select(s =>
        {
            var booked = bookedSlots.GetValueOrDefault(s.Start, 0);
            var available = Math.Max(0, s.MaxPerSlot - booked);
            return new TimeSlotDto(
                s.Start.ToString(@"hh\:mm"),
                s.End.ToString(@"hh\:mm"),
                available,
                available > 0);
        }).ToList();
    }
}
