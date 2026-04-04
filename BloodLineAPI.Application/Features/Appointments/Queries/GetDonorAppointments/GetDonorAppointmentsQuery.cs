using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.MobileAppointment;
using MediatR;


namespace BloodLineAPI.Application.Features.Appointments.Queries.DonorAppointments
{
   
    public sealed record GetDonorAppointmentsQuery(Guid DonorId, string Status)
    : IRequest<Result<IReadOnlyList<AppointmentListItem>>>;
    
}
