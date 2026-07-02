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
    IMediator mediator,
    IDateTimeProvider dateTimeProvider,
    IBackgroundNotificationService backgroundNotificationService)
    : IRequestHandler<SystemCancelAppointmentCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SystemCancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.DonationAppointments
            .Include(a => a.Donor)
            .Include(a => a.DonationCenter)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("DonationAppointment", request.AppointmentId);

        // Cancel with gracePeriodMinutes = 0 to allow staff/doctors to cancel anytime
        appointment.Cancel(request.Reason?.Trim() ?? "Cancelled by staff", dateTimeProvider.LocalNow, gracePeriodMinutes: 0);

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

        // Enqueue background push notification to the donor
        try
        {
            var payload = new Dictionary<string, string>
            {
                ["targetEntity"] = "DonationAppointment",
                ["targetId"] = appointment.Id.ToString()
            };

            var reasonStr = request.Reason?.Trim() ?? "إلغاء بواسطة المركز";

            backgroundNotificationService.EnqueueNotification(
                appointment.DonorId,
                "إلغاء موعد التبرع",
                $"عزيزي المتبرع، نود إعلامك بأنه قد تم إلغاء موعد تبرعك بالدم في {appointment.DonationCenter.Name} بتاريخ {appointment.ScheduledDate:yyyy-MM-dd} الساعة {appointment.StartTime:hh\\:mm} بسبب: {reasonStr}.",
                NotificationType.AppointmentCancelled,
                payload);
        }
        catch
        {
            // Ignore push notification failures to keep response safe
        }

        return Result<string>.Success("Appointment cancelled successfully.");
    }
}
