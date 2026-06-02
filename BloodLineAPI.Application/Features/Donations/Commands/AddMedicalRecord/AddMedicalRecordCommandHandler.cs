using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Enums;
using BloodLineAPI.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donations.Commands.AddMedicalRecord;

public sealed class AddMedicalRecordCommandHandler(
    ICurrentUserService currentUserService,
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<AddMedicalRecordCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        AddMedicalRecordCommand request,
        CancellationToken cancellationToken)
    {
        var donation = await dbContext.DonationAppointments
            .Include(da => da.Donor)
            .FirstOrDefaultAsync(da => da.Id == request.DonationId, cancellationToken);

        if (donation == null)
        {
            return Result<string>.Failure("Donation not found.");
        }

        if (donation.DonationStatus != DonationStatus.Pending)
        {
            return Result<string>.Failure("Medical screening can only be added to pending donations.");
        }

        var donor = donation.Donor;
        if (donor == null)
        {
            return Result<string>.Failure("Donor not found for this donation.");
        }

        var donationType = request.DonationType.Trim().ToLowerInvariant() switch
        {
            "plasma" => DonationType.Plasma,
            "platelets" => DonationType.Platelets,
            _ => DonationType.WholeBlood
        };

        donation.DonationType = donationType;

        if (!string.IsNullOrWhiteSpace(request.BloodType))
        {
            var bloodTypeValue = request.BloodType.Trim().ToUpperInvariant();
            var groupStr = bloodTypeValue[..^1];
            var sign = bloodTypeValue[^1];

            if (Enum.TryParse<BloodGroupName>(groupStr, true, out var groupName))
            {
                var rhFactor = sign == '+' ? RhFactor.Positive : RhFactor.Negative;
                var bloodType = await dbContext.BloodTypes
                    .FirstOrDefaultAsync(bt => bt.BloodGroupName == groupName && bt.RhFactor == rhFactor, cancellationToken);

                if (bloodType != null)
                {
                    donor.BloodTypeId = bloodType.Id;
                }
            }
        }

        // Get current staff ID (doctor)
        var doctorUserId = Guid.Parse(currentUserService.UserId!);

        // Parse blood pressure
        var bpParts = request.AdditionalData.BloodPressure.Split('/');
        var systolic = decimal.Parse(bpParts[0]);
        var diastolic = decimal.Parse(bpParts[1]);

        // Parse lockout date if not eligible (deferred or ineligible)
        DateTime? lockoutUntil = null;
        if (!request.Status.Equals("eligible", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(request.DeferredUntil))
        {
            lockoutUntil = DateTime.Parse(request.DeferredUntil).ToUniversalTime();
        }

        // Create Medical Screening
        var screening = new MedicalScreening
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            PerformedByStaffId = doctorUserId,
            ScreeningDate = dateTimeProvider.LocalNow,
            Weight = request.AdditionalData.Weight,
            SystolicBP = systolic,
            DiastolicBP = diastolic,
            HemoglobinLevel = request.AdditionalData.Hemoglobin,
            Temperature = 0, // Defaulting as not sent by mobile/frontend contract
            PulseRate = 0,    // Defaulting as not sent by mobile/frontend contract
            IsEligible = request.Status.Equals("eligible", StringComparison.OrdinalIgnoreCase),
            HasChronicDiseases = request.Diseases != null && request.Diseases.Length > 0,
            ChronicDiseaseDetails = request.Diseases != null && request.Diseases.Length > 0 
                ? JsonSerializer.Serialize(request.Diseases) 
                : null,
            IsAllergic = request.IsAllergic,
            RejectionReason = request.RejectionReason,
            LockoutUntil = lockoutUntil,
            DonationAppointmentId = donation.Id
        };

        await dbContext.MedicalScreenings.AddAsync(screening, cancellationToken);

        // Update Donor status and raise events
        var oldDonorStatus = donor.Status;
        var newDonorStatus = request.Status.ToLowerInvariant() switch
        {
            "deferred" => DonorStatus.Deferred,
            "ineligible" => DonorStatus.Ineligible,
            _ => DonorStatus.Eligible
        };

        if (donor.Status != newDonorStatus)
        {
            donor.Status = newDonorStatus;
            donor.AddDomainEvent(new DonorStatusChangedEvent(
                donor.Id,
                oldDonorStatus,
                newDonorStatus,
                request.RejectionReason ?? "Medical screening results",
                dateTimeProvider.LocalNow));
        }

        // Update Donation status
        if (screening.IsEligible)
        {
            donation.AttachMedicalScreening(screening.Id, dateTimeProvider.LocalNow);
        }
        else
        {
            donation.RejectAfterScreening(screening.Id, dateTimeProvider.LocalNow, screening.RejectionReason);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(donation.DonationCode);
    }
}
