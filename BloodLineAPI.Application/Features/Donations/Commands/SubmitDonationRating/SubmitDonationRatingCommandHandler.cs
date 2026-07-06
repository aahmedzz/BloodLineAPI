using System;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donations.Commands.SubmitDonationRating;

public sealed class SubmitDonationRatingCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<SubmitDonationRatingCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(SubmitDonationRatingCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch donor profile
        var donor = await dbContext.Donors
            .FirstOrDefaultAsync(d => d.Id == request.UserId, cancellationToken);

        if (donor == null)
        {
            return Result<Unit>.Failure("Donor profile not found.");
        }

        // 2. Fetch the donation appointment
        var appointment = await dbContext.DonationAppointments
            .FirstOrDefaultAsync(a => a.Id == request.DonationId, cancellationToken);

        if (appointment == null)
        {
            return Result<Unit>.Failure("Donation appointment not found.");
        }

        // 3. Validation guards
        if (appointment.DonorId != donor.Id)
        {
            return Result<Unit>.Failure("Unauthorized: You can only rate your own donations.");
        }

        if (appointment.DonationStatus != DonationStatus.Completed)
        {
            return Result<Unit>.Failure("You can only rate a donation that has been completed.");
        }

        if (request.StarScore < 1 || request.StarScore > 5)
        {
            return Result<Unit>.Failure("Rating score must be between 1 and 5 stars.");
        }

        if (request.FeedbackText?.Length > 500)
        {
            return Result<Unit>.Failure("Feedback cannot exceed 500 characters.");
        }

        // 4. Duplicate rating guard
        var existingRating = await dbContext.DonationRatings
            .AnyAsync(r => r.DonationAppointmentId == request.DonationId, cancellationToken);

        if (existingRating)
        {
            return Result<Unit>.Failure("This donation has already been rated.");
        }

        // 5. Save rating
        var rating = new DonationRating
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            DonationAppointmentId = request.DonationId,
            StarScore = request.StarScore,
            FeedbackText = request.FeedbackText?.Trim(),
            SubmittedAt = dateTimeProvider.UtcNow
        };

        await dbContext.DonationRatings.AddAsync(rating, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
