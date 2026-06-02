using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Appointments.Commands.MarkAppointmentNoShow;

public sealed class MarkAppointmentNoShowCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<MarkAppointmentNoShowCommand, Result<string>>
{
    public async Task<Result<string>> Handle(MarkAppointmentNoShowCommand request, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.DonationAppointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("DonationAppointment", request.AppointmentId);

        try
        {
            appointment.MarkNoShow();
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<string>.Success("Appointment marked as no-show successfully.");
        }
        catch (DomainException ex)
        {
            return Result<string>.Failure(ex.Message);
        }
    }
}
