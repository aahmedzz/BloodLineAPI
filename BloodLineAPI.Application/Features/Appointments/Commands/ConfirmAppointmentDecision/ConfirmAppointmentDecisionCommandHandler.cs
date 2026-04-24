using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Appointments.Commands.ConfirmAppointmentDecision;

public sealed class ConfirmAppointmentDecisionCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ConfirmAppointmentDecisionCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ConfirmAppointmentDecisionCommand request, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.DonationAppointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId && a.DonorId == request.DonorId, cancellationToken)
            ?? throw new NotFoundException("DonationAppointment", request.AppointmentId);

        if (appointment.Status != AppointmentStatus.Pending)
        {
            return Result<string>.Failure("Only pending appointments can be confirmed or cancelled from this step.");
        }

        if (request.IsConfirmed)
        {
            appointment.Confirm();
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<string>.Success("Appointment confirmed successfully.");
        }

        dbContext.DonationAppointments.Remove(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<string>.Success("Appointment cancelled successfully.");
    }
}
