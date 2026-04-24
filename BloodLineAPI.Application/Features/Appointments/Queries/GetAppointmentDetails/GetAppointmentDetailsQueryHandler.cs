using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetAppointmentDetails;

public sealed class GetAppointmentDetailsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAppointmentDetailsQuery, AppointmentDetailsDto>
{
    public async Task<AppointmentDetailsDto> Handle(GetAppointmentDetailsQuery request, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.DonationAppointments
            .AsNoTracking()
            .Include(a => a.DonationCenter)
            .Include(a => a.HealthPreScreening)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId && a.DonorId == request.DonorId, cancellationToken)
            ?? throw new NotFoundException("DonationAppointment", request.AppointmentId);

        var center = appointment.DonationCenter;

        var centerDto = new AppointmentDonationCenterDto(
            center.Id,
            center.Name,
            center.Location,
            center.AddressDetails,
            center.Latitude,
            center.Longitude,
            center.CenterType.ToString(),
            center.Status.ToString(),
            $"{center.StartTime:hh\\:mm} - {center.EndTime:hh\\:mm}");

        HealthPreScreeningSummaryDto? screeningDto = null;
        if (appointment.HealthPreScreening is not null)
        {
            screeningDto = new HealthPreScreeningSummaryDto(
                appointment.HealthPreScreening.Id,
                appointment.HealthPreScreening.IsEligible,
                appointment.HealthPreScreening.ScreenedAt);
        }

        return new AppointmentDetailsDto(
            appointment.Id,
            appointment.ScheduledDate,
            appointment.StartTime.ToString(@"hh\:mm"),
            appointment.EndTime.ToString(@"hh\:mm"),
            appointment.DonationType.ToString(),
            appointment.Status.ToString(),
            appointment.CancellationReason,
            centerDto,
            screeningDto);
    }
}
