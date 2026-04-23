using BloodLineAPI.Application.Common.Exceptions;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BloodLineAPI.Application.Features.Appointments.Commands.CreateAppointment;

public sealed class CreateAppointmentCommandHandler(
    IApplicationDbContext dbContext,
    IOptions<DonationCooldownSettings> cooldownOptions)
    : IRequestHandler<CreateAppointmentCommand, Result<CreateAppointmentResultDto>>
{
    public async Task<Result<CreateAppointmentResultDto>> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var center = await dbContext.DonationCenters
            .Include(c => c.OpeningHours)
            .Include(c => c.CenterExclusions)
            .FirstOrDefaultAsync(c => c.Id == request.DonationCenterId, cancellationToken)
            ?? throw new NotFoundException(nameof(DonationCenter), request.DonationCenterId);

        if (!center.IsOperatingOn(request.ScheduledDate))
        {
            return Result<CreateAppointmentResultDto>.Failure("The center is not operating on the selected date.");
        }

        var operatingHours = center.ResolveOperatingHours(
            request.ScheduledDate, center.CenterExclusions.ToList(), center.OpeningHours.ToList());
        if (operatingHours is null)
        {
            return Result<CreateAppointmentResultDto>.Failure("The center is closed on the selected date.");
        }

        var (open, close, maxPerSlot) = operatingHours.Value;
        if (request.StartTime < open || request.StartTime >= close)
        {
            return Result<CreateAppointmentResultDto>.Failure("Selected time is outside center operating hours.");
        }

        var bookingCount = await dbContext.DonationAppointments
            .Where(a => a.DonationCenterId == request.DonationCenterId)
            .Where(a => a.ScheduledDate == request.ScheduledDate.Date && a.StartTime == request.StartTime)
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .CountAsync(cancellationToken);

        var donor = await dbContext.Donors.FirstOrDefaultAsync(d => d.Id == request.DonorId, cancellationToken)
            ?? throw new NotFoundException(nameof(Donor), request.DonorId);

        // Query active lockout using the correct DonorId FK + LockoutUntil
        var activeLockout = await dbContext.MedicalScreenings
            .Where(ms => ms.DonorId == request.DonorId && !ms.IsEligible)
            .Where(ms => ms.LockoutUntil != null && ms.LockoutUntil > DateTime.UtcNow)
            .OrderByDescending(ms => ms.LockoutUntil)
            .Select(ms => ms.LockoutUntil)
            .FirstOrDefaultAsync(cancellationToken);

        var slotDuration = center.SlotDurationMinutes ?? 15;

        var appointment = DonationAppointment.Book(
            request.DonorId,
            center.Id,
            request.ScheduledDate,
            request.StartTime,
            slotDuration,
            request.DonationType,
            null,
            bookingCount,
            maxPerSlot,
            donor.LastDonationDate,
            activeLockout,
            donor.Gender,
            cooldownOptions.Value);

        dbContext.DonationAppointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateAppointmentResultDto>.Success(new CreateAppointmentResultDto(
            appointment.Id,
            appointment.ScheduledDate,
            appointment.StartTime.ToString(@"hh\:mm"),
            appointment.EndTime.ToString(@"hh\:mm"),
            appointment.DonationType.ToString(),
            center.Name,
            appointment.Status.ToString()));
    }
}
