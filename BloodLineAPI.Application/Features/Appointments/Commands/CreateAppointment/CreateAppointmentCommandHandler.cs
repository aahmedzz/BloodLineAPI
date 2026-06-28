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
    IDynamicSettingsService dynamicSettingsService,
    IDonorEligibilityService eligibilityService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CreateAppointmentCommand, Result<CreateAppointmentResultDto>>
{
    public async Task<Result<CreateAppointmentResultDto>> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var center = await dbContext.DonationCenters
            .Include(c => c.OpeningHours)
            .Include(c => c.CenterExclusions)
            .FirstOrDefaultAsync(c => c.Id == request.DonationCenterId, cancellationToken)
            ?? throw new NotFoundException(nameof(DonationCenter), request.DonationCenterId);

        var operatingHours = center.ResolveOperatingHours(
            request.ScheduledDate, center.CenterExclusions.ToList(), center.OpeningHours.ToList());
        if (operatingHours is null)
        {
            return Result<CreateAppointmentResultDto>.Failure("The center is closed on the selected date.");
        }

        var (open, close, maxPerSlot) = operatingHours.Value;

        if (DateOnly.FromDateTime(request.ScheduledDate) < dateTimeProvider.CurrentLocalDate)
        {
            return Result<CreateAppointmentResultDto>.Failure("Cannot book an appointment in the past.");
        }

        if (DateOnly.FromDateTime(request.ScheduledDate) == dateTimeProvider.CurrentLocalDate && HasSlotPassed(request.StartTime, dateTimeProvider.CurrentLocalTimeOfDay, open, close))
        {
            return Result<CreateAppointmentResultDto>.Failure("Cannot book a time slot that has already passed.");
        }

        if (!center.IsOperatingOn(request.ScheduledDate))
        {
            return Result<CreateAppointmentResultDto>.Failure("The center is not operating on the selected date.");
        }

        if (!IsTimeInOperatingInterval(request.StartTime, open, close))
        {
            return Result<CreateAppointmentResultDto>.Failure("Selected time is outside center operating hours.");
        }

        if (!center.SupportsDonationType(request.DonationType))
        {
            var availableTypes = string.Join(", ", center.GetSupportedDonationTypes());
            return Result<CreateAppointmentResultDto>.Failure($"This center does not support {request.DonationType} donation. Available types: {availableTypes}.");
        }

        var bookingCount = await dbContext.DonationAppointments
            .Where(a => a.DonationCenterId == request.DonationCenterId)
            .Where(a => a.ScheduledDate == request.ScheduledDate.Date && a.StartTime == request.StartTime)
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .CountAsync(cancellationToken);

        var donor = await dbContext.Donors.FirstOrDefaultAsync(d => d.Id == request.DonorId, cancellationToken)
            ?? throw new NotFoundException(nameof(Donor), request.DonorId);

        // Check if the donor already has an upcoming/active booking (Pending or Confirmed)
        var hasUpcomingBooking = await dbContext.DonationAppointments
            .AnyAsync(a => a.DonorId == request.DonorId &&
                           (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed),
                       cancellationToken);

        if (hasUpcomingBooking)
        {
            return Result<CreateAppointmentResultDto>.Failure("You already have an active or upcoming appointment booked.");
        }

        // Centralised eligibility check (lockout, cooldown, status)
        var eligibility = await eligibilityService.CheckEligibilityAsync(
            donor.Id, request.DonationType, cancellationToken);

        if (!eligibility.IsSuccess)
        {
            return Result<CreateAppointmentResultDto>.Failure(eligibility.Error!);
        }

        if (!eligibility.Data!.IsEligible)
        {
            return Result<CreateAppointmentResultDto>.Failure(eligibility.Data.RejectionReason!);
        }

        // Query active lockout for domain-level Book() guard (kept for defence-in-depth)
        var activeLockout = eligibility.Data.DeferredUntil;

        var slotDuration = center.SlotDurationMinutes ?? 15;

        var dynamicSettings = await dynamicSettingsService.GetSettingsAsync(cancellationToken);
        var cooldownSettings = new DonationCooldownSettings
        {
            WholeBloodMaleDays = dynamicSettings.WholeBloodMaleDays,
            WholeBloodFemaleDays = dynamicSettings.WholeBloodFemaleDays,
            PlasmaDays = dynamicSettings.PlasmaDays,
            PlateletsDays = dynamicSettings.PlateletsDays
        };

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
            open,
            close,
            donor.LastDonationDate,
            activeLockout,
            donor.Gender,
            cooldownSettings,
            dateTimeProvider.LocalNow,
            source: DonationSource.MobileApp);

        if (request.UrgentBloodAppealId.HasValue)
        {
            appointment.SetUrgentBloodAppeal(request.UrgentBloodAppealId.Value);
        }

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

    private static bool IsTimeInOperatingInterval(TimeSpan time, TimeSpan open, TimeSpan close)
    {
        if (open <= close)
        {
            return time >= open && time < close;
        }
        else
        {
            return time >= open || time < close;
        }
    }

    private static bool HasSlotPassed(TimeSpan startTime, TimeSpan currentTime, TimeSpan open, TimeSpan close)
    {
        if (open <= close)
        {
            return currentTime > startTime;
        }
        else
        {
            if (startTime >= open)
            {
                return currentTime >= open && currentTime > startTime;
            }
            else
            {
                return currentTime < open && currentTime > startTime;
            }
        }
    }
}
