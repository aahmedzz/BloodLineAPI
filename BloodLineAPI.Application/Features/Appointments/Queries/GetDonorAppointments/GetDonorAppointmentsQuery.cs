using MediatR;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetDonorAppointments;

public sealed record GetDonorAppointmentsQuery(bool UpcomingOnly = true) : IRequest<IReadOnlyList<AppointmentListItemDto>>
{
    [JsonIgnore]
    public Guid DonorId { get; init; }
}
