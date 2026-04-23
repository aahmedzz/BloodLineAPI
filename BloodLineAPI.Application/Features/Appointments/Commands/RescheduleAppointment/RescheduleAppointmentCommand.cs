using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using MediatR;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Appointments.Commands.RescheduleAppointment;

public sealed record RescheduleAppointmentCommand(
    Guid AppointmentId,
    DateTime NewScheduledDate,
    TimeSpan NewStartTime) : IRequest<Result<CreateAppointmentResultDto>>
{
    [JsonIgnore]
    public Guid DonorId { get; init; }
}
