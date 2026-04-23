using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BloodLineAPI.Application.Features.Appointments.Commands.RescheduleAppointment;

public sealed class RescheduleAppointmentCommandHandler(
    IApplicationDbContext dbContext,
    IOptions<AppointmentSettings> appointmentSettings)
    : IRequestHandler<RescheduleAppointmentCommand, Result<CreateAppointmentResultDto>>
{
    public async Task<Result<CreateAppointmentResultDto>> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.DonationAppointments
            .Include(a => a.DonationCenter)
                .ThenInclude(c => c.OpeningHours)
            .Include(a => a.DonationCenter)
                .ThenInclude(c => c.CenterExclusions)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId && a.DonorId == request.DonorId, cancellationToken)
            ?? throw new NotFoundException("DonationAppointment", request.AppointmentId);

        var center = appointment.DonationCenter;
        if (!center.IsOperatingOn(request.NewScheduledDate))
        {
            return Result<CreateAppointmentResultDto>.Failure("The center is not operating on the selected date.");
        }

        var hours = center.ResolveOperatingHours(
            request.NewScheduledDate, center.CenterExclusions.ToList(), center.OpeningHours.ToList());
        if (hours is null)
        {
            return Result<CreateAppointmentResultDto>.Failure("The center is closed on the selected date.");
        }

        var (open, close, maxPerSlot) = hours.Value;
        if (request.NewStartTime < open || request.NewStartTime >= close)
        {
            return Result<CreateAppointmentResultDto>.Failure("Selected time is outside center operating hours.");
        }

        var bookingCount = await dbContext.DonationAppointments
            .Where(a => a.Id != appointment.Id)
            .Where(a => a.DonationCenterId == appointment.DonationCenterId)
            .Where(a => a.ScheduledDate == request.NewScheduledDate.Date && a.StartTime == request.NewStartTime)
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .CountAsync(cancellationToken);

        var slotDuration = center.SlotDurationMinutes ?? 15;

        appointment.Reschedule(
            center.Id,
            request.NewScheduledDate,
            request.NewStartTime,
            slotDuration,
            bookingCount,
            maxPerSlot,
            appointmentSettings.Value.GracePeriodMinutes);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateAppointmentResultDto>.Success(new CreateAppointmentResultDto(
            appointment.Id,
            appointment.ScheduledDate,
            appointment.StartTime.ToString(@"hh\:mm"),
            appointment.EndTime.ToString(@"hh\:mm"),
            appointment.DonationType.ToString(),
            center.Name,
            appointment.Status.ToString()));
    }
}
