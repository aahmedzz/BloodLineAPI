using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BloodLineAPI.Application.Features.Appointments.Commands.CancelAppointment;

public sealed class CancelAppointmentCommandHandler(
    IApplicationDbContext dbContext,
    IOptions<AppointmentSettings> appointmentSettings)
    : IRequestHandler<CancelAppointmentCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.DonationAppointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId && a.DonorId == request.DonorId, cancellationToken)
            ?? throw new NotFoundException("DonationAppointment", request.AppointmentId);

        appointment.Cancel(request.Reason?.Trim() ?? "Cancelled by donor", appointmentSettings.Value.GracePeriodMinutes);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<string>.Success("Appointment cancelled successfully.");
    }
}
