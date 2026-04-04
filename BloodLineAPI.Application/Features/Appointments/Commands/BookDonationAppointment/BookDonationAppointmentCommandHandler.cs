using BloodLineAPI.Application.Common.Extentions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Appointments.Commands.BookDonationAppointment
{
    public sealed class BookDonationAppointmentCommandHandler(IApplicationDbContext db)
        : IRequestHandler<BookDonationAppointmentCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(BookDonationAppointmentCommand request, CancellationToken cancellationToken)
        {
            var donor = await db.Donors
                .FirstOrDefaultAsync(d => d.Id == request.DonorId, cancellationToken);

            if (donor is null)
                return Result<Guid>.Failure("Donor not found.");

            var center = await db.DonationCenters
                .FirstOrDefaultAsync(c => c.Id == request.DonationCenterId && c.Status == "Active", cancellationToken);

            if (center is null)
                return Result<Guid>.Failure("Invalid or inactive donation center.");

            var eligibility = donor.IsEligibleForDonation(request.PrescreeningAnswers);

            if (!eligibility.Eligible)
                return Result<Guid>.Failure(eligibility.Message ?? "You are not eligible for donation.");

            var lastDonation = await db.DonationAppointments
                .Where(a => a.DonorId == request.DonorId && a.Status == AppointmentStatus.Completed)
                .OrderByDescending(a => a.ScheduledDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastDonation != null &&(request.ScheduledDate - lastDonation.ScheduledDate).TotalDays < 90)
            {
                return Result<Guid>.Failure("You must wait before your next donation.");
            }

            var isSlotTaken = await db.DonationAppointments
                .AnyAsync(a =>
                    a.DonationCenterId == request.DonationCenterId &&
                    a.ScheduledDate == request.ScheduledDate.Date, cancellationToken);

            if (isSlotTaken)
                return Result<Guid>.Failure("This time slot is already booked.");

            var entity = new DonationAppointment
            {
                Id = Guid.NewGuid(),
                DonorId = request.DonorId,
                DonationCenterId = request.DonationCenterId,
                ScheduledDate = request.ScheduledDate.Date,
                BookTime = request.BookTime,
                DonationType = request.DonationType,
                Status = AppointmentStatus.Pending
            };

            await db.DonationAppointments.AddAsync(entity, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(entity.Id);
        }
    }
}