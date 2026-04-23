using BloodLineAPI.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Appointments.Commands.SubmitHealthPreScreening;

public sealed record SubmitHealthPreScreeningCommand(
    Guid AppointmentId,
    bool HasChronicDisease,
    bool HasRecentSurgery,
    bool IsTakingMedication,
    bool HasRecentTattooOrPiercing,
    bool HasRecentInfection,
    bool IsPregnantOrBreastfeeding,
    bool HasBleedingDisorder,
    bool HasRecentVaccination) : IRequest<Result<HealthPreScreeningResultDto>>
{
    [JsonIgnore]
    public Guid DonorId { get; init; }
}
