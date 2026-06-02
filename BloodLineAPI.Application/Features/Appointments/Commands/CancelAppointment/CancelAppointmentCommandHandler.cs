using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Events;
using BloodLineAPI.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Application.Features.Appointments.Commands.CancelAppointment;

public sealed class CancelAppointmentCommandHandler(
    IApplicationDbContext dbContext,
    IOptions<AppointmentSettings> appointmentSettings,
    IMediator mediator,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CancelAppointmentCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.DonationAppointments
            .Include(a => a.Donor)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId && a.DonorId == request.DonorId, cancellationToken)
            ?? throw new NotFoundException("DonationAppointment", request.AppointmentId);

        var wasConfirmed = appointment.Status == AppointmentStatus.Confirmed;

        appointment.Cancel(request.Reason?.Trim() ?? "Cancelled by donor", dateTimeProvider.LocalNow, appointmentSettings.Value.GracePeriodMinutes);

        await dbContext.SaveChangesAsync(cancellationToken);

        // Publish event for real-time dashboard notifications
        await mediator.Publish(new SystemAppointmentCancelledEvent(
            appointment.Id,
            appointment.DonationCenterId,
            appointment.Donor.FullName,
            appointment.StartTime,
            appointment.ScheduledDate,
            appointment.CancellationReason,
            appointment.CancelledAt,
            IsCancelledByDonor: wasConfirmed
        ), cancellationToken);

        return Result<string>.Success("Appointment cancelled successfully.");
    }
}
