using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.DonationEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Appointments.Commands.SubmitHealthPreScreening;

public sealed class SubmitHealthPreScreeningCommandHandler(IApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    : IRequestHandler<SubmitHealthPreScreeningCommand, Result<HealthPreScreeningResultDto>>
{
    public async Task<Result<HealthPreScreeningResultDto>> Handle(SubmitHealthPreScreeningCommand request, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.DonationAppointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId && a.DonorId == request.DonorId, cancellationToken);

        if (appointment is null)
        {
            return Result<HealthPreScreeningResultDto>.Failure("Appointment was not found for this donor.");
        }

        var screening = HealthPreScreening.Create(
            request.DonorId,
            request.HasBeenThreeToFourMonthsSinceLastDonation,
            request.HasAnyDisqualifyingCondition,
            request.IsTakingBloodThinnersOrCriticalMedication,
            request.HasRecentSurgeryInPast6Months,
            request.HasRecentTattooOrPiercingInPast6Months,
            request.HasDentalProcedureInPastWeek,
            request.HasCurrentFeverInfectionOrSevereCold,
            request.HasChronicIllnessAffectingBloodDonation,
            dateTimeProvider.LocalNow);

        dbContext.HealthPreScreenings.Add(screening);
        appointment.AttachHealthPreScreening(request.DonorId, screening.Id);

        var appointmentCancelled = false;
        string? ineligibilityReason = null;
        string? recommendation = null;

        if (!screening.IsEligible)
        {
            (ineligibilityReason, recommendation) = BuildIneligibilityContent(request);
            appointment.CancelDueToIneligiblePreScreening(dateTimeProvider.LocalNow);
            appointmentCancelled = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<HealthPreScreeningResultDto>.Success(
            new HealthPreScreeningResultDto(
                screening.Id,
                screening.IsEligible,
                ineligibilityReason,
                recommendation,
                appointmentCancelled));
    }

    private static (string Reason, string Recommendation) BuildIneligibilityContent(SubmitHealthPreScreeningCommand request)
    {
        if (!request.HasBeenThreeToFourMonthsSinceLastDonation)
        {
            return (
                "Your last blood donation was too recent.",
                "Please wait until at least 3–4 months have passed since your last donation, then try the eligibility check again.");
        }

        if (request.IsTakingBloodThinnersOrCriticalMedication)
        {
            return (
                "You are currently taking medication that may affect donation safety.",
                "Please consult your doctor and return after the medication period is completed and approved for donation.");
        }

        if (request.HasCurrentFeverInfectionOrSevereCold)
        {
            return (
                "You currently have a fever, infection, or severe cold.",
                "Please recover fully and wait the medically advised period before trying the eligibility check again.");
        }

        if (request.HasRecentSurgeryInPast6Months)
        {
            return (
                "You had recent surgery.",
                "Please wait at least 6 months after surgery, or follow your doctor’s advice, before donating.");
        }

        if (request.HasRecentTattooOrPiercingInPast6Months)
        {
            return (
                "You had a recent tattoo or piercing.",
                "Please wait at least 6 months from the procedure date before donating.");
        }

        if (request.HasDentalProcedureInPastWeek)
        {
            return (
                "You recently had a dental procedure.",
                "Please wait at least 7 days after your dental procedure before donating.");
        }

        if (request.HasChronicIllnessAffectingBloodDonation)
        {
            return (
                "You have a chronic illness that may affect blood donation.",
                "Please consult your doctor and the donation center to confirm eligibility before reapplying.");
        }

        if (request.HasAnyDisqualifyingCondition)
        {
            return (
                "One or more disqualifying medical conditions were reported.",
                "Please contact the donation center for medical guidance before attempting to donate again.");
        }

        return (
            "You are not eligible to donate right now.",
            "Please wait for the advised period and try the eligibility check again.");
    }
}
