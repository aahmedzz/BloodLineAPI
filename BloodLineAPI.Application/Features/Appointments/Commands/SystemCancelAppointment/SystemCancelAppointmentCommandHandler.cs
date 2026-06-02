using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Events;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Appointments.Commands.SystemCancelAppointment;

public sealed class SystemCancelAppointmentCommandHandler(
    IApplicationDbContext dbContext,
    IMediator mediator)
    : IRequestHandler<SystemCancelAppointmentCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SystemCancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.DonationAppointments
            .Include(a => a.Donor)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("DonationAppointment", request.AppointmentId);

        // Cancel with gracePeriodMinutes = 0 to allow staff/doctors to cancel anytime
        appointment.Cancel(request.Reason?.Trim() ?? "Cancelled by staff", gracePeriodMinutes: 0);

        await dbContext.SaveChangesAsync(cancellationToken);

        // Publish event to trigger SignalR notification
        await mediator.Publish(new SystemAppointmentCancelledEvent(
            appointment.Id,
            appointment.DonationCenterId,
            appointment.Donor.FullName,
            appointment.StartTime,
            appointment.ScheduledDate,
            appointment.CancellationReason,
            appointment.CancelledAt,
            IsCancelledByDonor: false
        ), cancellationToken);

        return Result<string>.Success("Appointment cancelled successfully.");
    }
}
