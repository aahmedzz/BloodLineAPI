using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.MobileAppointment;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetDonorAppointmentById
{

    public sealed class GetDonorAppointmentByIdQueryHandler(IApplicationDbContext db)
    :IRequestHandler<GetDonorAppointmentByIdQuery, Result<AppointmentDetail>>
    {
        public async Task<Result<AppointmentDetail>> Handle(GetDonorAppointmentByIdQuery request,CancellationToken cancellationToken)
        {
            var dto = await db.DonationAppointments
                .AsNoTracking()
                .Where(a => a.Id == request.AppointmentId && a.DonorId == request.DonorId)
                .Select(a => new AppointmentDetail(
                    a.Id,
                    a.DonationType.ToString(),
                    a.ScheduledDate,
                    a.BookTime,
                    a.Status,
                    a.DonationCenterId,
                    a.DonationCenter.Name,
                    a.DonationCenter.Location,
                    a.DonationCenter.AddressDetails))
                .FirstOrDefaultAsync(cancellationToken);
            if (dto is null)
                return Result<AppointmentDetail>.Failure("Appointment not found.");
            return Result<AppointmentDetail>.Success(dto);
        }
    }
}

