using MediatR;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetAppointmentDetails;

public sealed record GetAppointmentDetailsQuery(Guid AppointmentId) : IRequest<AppointmentDetailsDto>
{
    [JsonIgnore]
    public Guid DonorId { get; init; }
}
