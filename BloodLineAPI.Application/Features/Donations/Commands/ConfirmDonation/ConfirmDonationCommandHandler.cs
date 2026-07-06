using System;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities.BloodEntities;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donations.Commands.ConfirmDonation;

public sealed class ConfirmDonationCommandHandler(
    ICurrentUserService currentUserService,
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IDynamicSettingsService dynamicSettingsService,
    IDonorStatusScheduler donorStatusScheduler,
    IBackgroundNotificationService backgroundNotificationService)
    : IRequestHandler<ConfirmDonationCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        ConfirmDonationCommand request,
        CancellationToken cancellationToken)
    {
        var donation = await dbContext.DonationAppointments
            .Include(da => da.Donor)
            .FirstOrDefaultAsync(da => da.Id == request.DonationId, cancellationToken);

        if (donation == null)
        {
            return Result<string>.Failure("Donation not found.");
        }

        // Idempotency check: only approved donations can proceed to confirmation
        if (donation.DonationStatus != DonationStatus.Approved)
        {
            if (donation.DonationStatus == DonationStatus.Completed)
            {
                return Result<string>.Failure("This donation has already been confirmed and completed.");
            }
            return Result<string>.Failure("Donation must be approved in medical screening before confirmation.");
        }

        var donor = donation.Donor;
        if (donor == null)
        {
            return Result<string>.Failure("Donor not found for this donation.");
        }

        var doctorUserId = Guid.Parse(currentUserService.UserId!);
        var now = dateTimeProvider.LocalNow;

        // Guard: DonationCode is a computed column — verify it was populated from DB
        if (string.IsNullOrEmpty(donation.DonationCode))
        {
            return Result<string>.Failure("Donation code is not available. Please retry.");
        }

        // Calculate Expiry Date & Volume based on Donation Type
        var expiryDate = donation.DonationType switch
        {
            DonationType.Plasma => now.AddDays(365),
            DonationType.Platelets => now.AddDays(5),
            _ => now.AddDays(42) // WholeBlood
        };

        var volume = donation.DonationType switch
        {
            DonationType.Plasma => 200m,
            DonationType.Platelets => 200m,
            _ => 450m // WholeBlood
        };

        // Create BloodBag record
        var bloodBag = new BloodBag
        {
            Id = Guid.NewGuid(),
            SerialNumber = donation.DonationCode,
            BloodTypeId = donor.BloodTypeId,
            CollectedByStaffId = doctorUserId,
            DonationAppointmentId = donation.Id,
            CollectionDate = now,
            ExpiryDate = expiryDate,
            Volume = volume,
            Status = BloodBagStatus.Testing,
            BagType = donation.DonationType
        };

        await dbContext.BloodBags.AddAsync(bloodBag, cancellationToken);

        // Update Donation Status through domain method (transitions DonationStatus -> Completed, raises event)
        donation.SendToLab(bloodBag.Id, now);

        // Update Donor stats
        donor.LastDonationDate = now;
        donor.TotalDonationCount += 1;

        // Perform atomic save
        await dbContext.SaveChangesAsync(cancellationToken);

        // Schedule Cooldown Expiry DonationReminder
        try
        {
            var settings = await dynamicSettingsService.GetSettingsAsync(cancellationToken);
            var cooldownDays = settings.GetCooldownDays(donation.DonationType, donor.Gender);
            var cooldownExpiryDate = now.Date.AddDays(cooldownDays);

            donorStatusScheduler.ScheduleCooldownReminder(donor.Id, cooldownExpiryDate);
        }
        catch
        {
            // Ignore scheduling failures to keep transaction safe
        }

        // Enqueue rating push notification
        try
        {
            var payload = new Dictionary<string, string>
            {
                ["targetEntity"] = "DonationAppointment",
                ["targetId"] = donation.Id.ToString(),
                ["action"] = "rate"
            };

            backgroundNotificationService.EnqueueNotification(
                donor.Id,
                "⭐ كيف كانت تجربتك في التبرع؟",
                "عزيزي المتبرع، نشكرك على تبرعك بالدم اليوم! يرجى أخذ لحظة لتقييم تجربتك في مركز التبرع لمساعدتنا في تحسين خدماتنا.",
                NotificationType.RateDonationCenter,
                payload);
        }
        catch
        {
            // Ignore push notification failures to keep transaction safe
        }

        return Result<string>.Success($"Donation confirmed successfully. Blood bag serial: {bloodBag.SerialNumber}");
    }
}
