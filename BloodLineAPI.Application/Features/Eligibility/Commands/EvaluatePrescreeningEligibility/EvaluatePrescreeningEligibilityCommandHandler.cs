using BloodLineAPI.Application.Common.Extentions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.PrescreeningEligibility;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace BloodLineAPI.Application.Features.Eligibility.Commands.EvaluatePrescreeningEligibility
{

    public sealed class EvaluatePrescreeningEligibilityCommandHandler(IApplicationDbContext db)
        : IRequestHandler<EvaluatePrescreeningEligibilityCommand, Result<PrescreeningEligibilityResponse>>
    {
        public async Task<Result<PrescreeningEligibilityResponse>> Handle(
            EvaluatePrescreeningEligibilityCommand request,
            CancellationToken cancellationToken)
        {
            var donor = await db.Donors
                                .AsNoTracking()
                                .FirstOrDefaultAsync(d => d.Id == request.DonorId, cancellationToken);
            if (donor is null)
                return Result<PrescreeningEligibilityResponse>.Failure("Donor not found.");
            var outcome = donor.IsEligibleForDonation(request.Answers);
            if (!outcome.Eligible)
            {
                var upcomingAppointments = await db.DonationAppointments
                    .Where(a => a.DonorId == request.DonorId && a.Status == AppointmentStatus.Pending)
                    .ToListAsync(cancellationToken);

                foreach (var appt in upcomingAppointments)
                    appt.Status = AppointmentStatus.Cancelled;

                await db.SaveChangesAsync(cancellationToken);
            }
            return Result<PrescreeningEligibilityResponse>.Success(outcome);
        }
    }
}
