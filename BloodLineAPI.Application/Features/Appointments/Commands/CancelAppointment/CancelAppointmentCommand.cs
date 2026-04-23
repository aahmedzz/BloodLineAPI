using BloodLineAPI.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Appointments.Commands.CancelAppointment;

public sealed record CancelAppointmentCommand(Guid AppointmentId, string? Reason) : IRequest<Result<string>>
{
    [JsonIgnore]
    public Guid DonorId { get; init; }
}
