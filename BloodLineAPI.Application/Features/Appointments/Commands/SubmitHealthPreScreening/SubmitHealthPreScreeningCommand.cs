using BloodLineAPI.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Appointments.Commands.SubmitHealthPreScreening;

public sealed record SubmitHealthPreScreeningCommand(
    Guid AppointmentId,
    bool HasBeenThreeToFourMonthsSinceLastDonation,
    bool HasAnyDisqualifyingCondition,
    bool IsTakingBloodThinnersOrCriticalMedication,
    bool HasRecentSurgeryInPast6Months,
    bool HasRecentTattooOrPiercingInPast6Months,
    bool HasDentalProcedureInPastWeek,
    bool HasCurrentFeverInfectionOrSevereCold,
    bool HasChronicIllnessAffectingBloodDonation) : IRequest<Result<HealthPreScreeningResultDto>>
{
    [JsonIgnore]
    public Guid DonorId { get; init; }
}
