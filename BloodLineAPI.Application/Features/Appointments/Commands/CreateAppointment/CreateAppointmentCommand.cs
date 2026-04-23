using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using BloodLineAPI.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Appointments.Commands.CreateAppointment;

public sealed record CreateAppointmentCommand(
    Guid DonationCenterId,
    DateTime ScheduledDate,
    TimeSpan StartTime,
    DonationType DonationType) : IRequest<Result<CreateAppointmentResultDto>>
{
    [JsonIgnore]
    public Guid DonorId { get; init; }
}
