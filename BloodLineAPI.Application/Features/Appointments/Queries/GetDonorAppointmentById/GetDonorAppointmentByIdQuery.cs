using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.MobileAppointment;
using MediatR;


namespace BloodLineAPI.Application.Features.Appointments.Queries.GetDonorAppointmentById
{
    
     public sealed record GetDonorAppointmentByIdQuery(Guid DonorId, Guid AppointmentId)
     : IRequest<Result<AppointmentDetail>>;
    
}
