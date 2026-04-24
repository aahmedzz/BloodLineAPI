using BloodLineAPI.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Appointments.Commands.ConfirmAppointmentDecision;

public sealed record ConfirmAppointmentDecisionCommand(Guid AppointmentId, bool IsConfirmed) : IRequest<Result<string>>
{
    [JsonIgnore]
    public Guid DonorId { get; init; }
}
