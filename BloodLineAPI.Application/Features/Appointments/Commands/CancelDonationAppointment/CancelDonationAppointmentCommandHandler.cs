using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Appointments.Commands.CancelDonationAppointment
{
    public sealed class CancelDonationAppointmentCommandHandler(IApplicationDbContext db)
        : IRequestHandler<CancelDonationAppointmentCommand, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(CancelDonationAppointmentCommand request, CancellationToken cancellationToken)
        {
            var appointment = await db.DonationAppointments
                .FirstOrDefaultAsync(
                    a => a.Id == request.AppointmentId && a.DonorId == request.DonorId,
                    cancellationToken);

            if (appointment is null)
                return Result<Unit>.Failure("Appointment not found.");

            if (appointment.Status == AppointmentStatus.Cancelled)
                return Result<Unit>.Success(Unit.Value);

            if (appointment.Status == AppointmentStatus.Completed)
                return Result<Unit>.Failure("A completed appointment cannot be cancelled.");

            if (appointment.ScheduledDate < DateTime.UtcNow.Date)
                return Result<Unit>.Failure("Cannot cancel past appointments.");

            appointment.Status = AppointmentStatus.Cancelled;

            await db.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}