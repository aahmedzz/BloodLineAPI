using BloodLineAPI.Application.Features.Appointments.Events;
using BloodLineAPI.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.EventHandlers;

public sealed class SystemAppointmentCancelledEventHandler(IHubContext<AppointmentsHub> hubContext)
    : INotificationHandler<SystemAppointmentCancelledEvent>
{
    public async Task Handle(SystemAppointmentCancelledEvent notification, CancellationToken cancellationToken)
    {
        // Only push notifications if the appointment was confirmed and cancelled by the donor
        if (!notification.IsCancelledByDonor)
        {
            return;
        }

        var isCancelledByDonor = notification.Reason != null && 
                                 notification.Reason.Contains("donor", System.StringComparison.OrdinalIgnoreCase);
        
        var payload = new
        {
            id = notification.AppointmentId,
            appointmentId = notification.AppointmentId, // Keep for backward compatibility
            donorName = notification.DonorName,
            time = notification.StartTime.ToString(@"hh\:mm"),
            date = notification.ScheduledDate.ToString("yyyy-MM-dd"),
            reason = notification.Reason,
            cancelledAt = notification.CancelledAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            cancelledByName = isCancelledByDonor ? notification.DonorName : "Staff"
        };

        // Broadcast to the center-specific group
        await hubContext.Clients.Group(notification.CenterId.ToString())
            .SendAsync("AppointmentCancelled", payload, cancellationToken);

        // Broadcast to the Global group (e.g. for system-wide admins)
        await hubContext.Clients.Group("Global")
            .SendAsync("AppointmentCancelled", payload, cancellationToken);
    }
}
