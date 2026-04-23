using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.DonationEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Appointments.Commands.SubmitHealthPreScreening;

public sealed class SubmitHealthPreScreeningCommandHandler(IApplicationDbContext dbContext)
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
            request.HasChronicDisease,
            request.HasRecentSurgery,
            request.IsTakingMedication,
            request.HasRecentTattooOrPiercing,
            request.HasRecentInfection,
            request.IsPregnantOrBreastfeeding,
            request.HasBleedingDisorder,
            request.HasRecentVaccination);

        dbContext.HealthPreScreenings.Add(screening);
        appointment.AttachHealthPreScreening(request.DonorId, screening.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<HealthPreScreeningResultDto>.Success(
            new HealthPreScreeningResultDto(screening.Id, screening.IsEligible));
    }
}
