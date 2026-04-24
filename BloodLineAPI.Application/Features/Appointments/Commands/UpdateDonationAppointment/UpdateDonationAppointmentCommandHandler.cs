using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Appointments.Commands.UpdateDonationAppointment
{
    public sealed class UpdateDonationAppointmentCommandHandler(IApplicationDbContext db)
        : IRequestHandler<UpdateDonationAppointmentCommand, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(UpdateDonationAppointmentCommand request, CancellationToken cancellationToken)
        {
            var appointment = await db.DonationAppointments
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId && a.DonorId == request.DonorId, cancellationToken);

            if (appointment is null)
                return Result<Unit>.Failure("Appointment not found.");

            if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
                return Result<Unit>.Failure("This appointment cannot be updated.");

            if (request.ScheduledDate.Date < DateTime.UtcNow.Date)
                return Result<Unit>.Failure("Cannot reschedule to a past date.");

            var isSlotTaken = await db.DonationAppointments
                .AnyAsync(a =>
                    a.Id != appointment.Id &&
                    a.DonationCenterId == appointment.DonationCenterId &&
                    a.ScheduledDate == request.ScheduledDate.Date,cancellationToken);

            if (isSlotTaken)
                return Result<Unit>.Failure("This time slot is already booked.");

            appointment.ScheduledDate = request.ScheduledDate.Date;
            appointment.BookTime = request.BookTime;
            appointment.DonationType = request.DonationType;

            await db.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}