using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.DonationEntities;
using BloodLineAPI.Domain.Entities.Users;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Donations.Commands.CreateDonation;

public sealed class CreateDonationCommandHandler(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IApplicationDbContext dbContext,
    IDonorEligibilityService eligibilityService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CreateDonationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateDonationCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Input Validation and Parsing
        var nationalIdClean = request.NationalId.Trim();
        var phoneClean = request.Phone.Trim();

        var nameParts = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (nameParts.Length < 3)
        {
            return Result<Guid>.Failure("Full name must contain at least 3 names.");
        }

        if (!DateOnly.TryParse(request.DateOfBirth, out var dateOfBirth))
        {
            return Result<Guid>.Failure("Invalid date of birth format.");
        }

        var genderEnum = request.Gender.Trim().Equals("male", StringComparison.OrdinalIgnoreCase) 
            ? Gender.Male 
            : Gender.Female;

        var addressClean = string.Join(", ", new[] { request.Governorate.Trim(), request.District.Trim(), request.Area?.Trim() }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        // 2. Load or Create Donor Profile
        var donor = await dbContext.Donors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.NationalId == nationalIdClean, cancellationToken);

        DonationAppointment? existingPendingDonation = null;
        bool isNewDonor = donor == null;

        if (donor != null)
        {
            // Check for any active donation across all centers (Pending or Approved)
            var activeDonation = await dbContext.DonationAppointments
                .FirstOrDefaultAsync(da => da.DonorId == donor.Id && 
                                          (da.DonationStatus == DonationStatus.Pending || da.DonationStatus == DonationStatus.Approved), 
                                     cancellationToken);

            if (activeDonation != null)
            {
                if (activeDonation.DonationStatus == DonationStatus.Approved)
                {
                    return Result<Guid>.Failure("هذا المتبرع لديه بالفعل تبرع معتمد قيد التنفيذ.");
                }
                existingPendingDonation = activeDonation;
            }

            // Verify eligibility for existing donor (always check, regardless of pending status, to catch post-booking ineligibility)
            var eligibilityResult = await CheckDonorEligibilityInternalAsync(donor.Id, cancellationToken);
            if (!eligibilityResult.IsSuccess)
            {
                return Result<Guid>.Failure(eligibilityResult.Error!);
            }

            // Update existing donor profile details
            UpdateExistingDonor(donor, request, phoneClean, nameParts, dateOfBirth, genderEnum, addressClean);
        }
        else
        {
            // Create new User & Donor profile
            var createResult = await CreateNewDonorAsync(request, nationalIdClean, phoneClean, nameParts, dateOfBirth, genderEnum, addressClean, cancellationToken);
            if (!createResult.IsSuccess)
            {
                return Result<Guid>.Failure(createResult.Error!);
            }
            donor = createResult.Data!;
        }

        // 3. Load and Validate Donation Center
        var center = await dbContext.DonationCenters
            .Include(c => c.OpeningHours)
            .Include(c => c.CenterExclusions)
            .FirstOrDefaultAsync(c => c.Id == request.DonationCenterId, cancellationToken);

        if (center == null)
        {
            return Result<Guid>.Failure("Donation center not found.");
        }

        if (center.CenterType == CenterType.Campaign && center.Status != CenterStatus.Active)
        {
            return Result<Guid>.Failure("حملة التبرع هذه ليست نشطة حالياً.");
        }

        var sourceEnum = request.Source.Trim().ToLowerInvariant() switch
        {
            "campaign" => DonationSource.Campaign,
            "mobileapp" => DonationSource.MobileApp,
            _ => DonationSource.WalkIn
        };

        var localNow = dateTimeProvider.LocalNow;
        if (sourceEnum != DonationSource.WalkIn && !center.IsOperatingOn(localNow))
        {
            return Result<Guid>.Failure("Donation center is closed today.");
        }

        // 4. Resolve Time Slot
        var slotResult = ResolveTimeSlot(center, existingPendingDonation, localNow, sourceEnum);
        if (!slotResult.IsSuccess)
        {
            return Result<Guid>.Failure(slotResult.Error!);
        }

        var (slotStart, slotEnd) = slotResult.Data;

        // 5. Update or Register Donation Appointment
        if (existingPendingDonation != null)
        {
            existingPendingDonation.UpdateSystemDonation(request.DonationCenterId, sourceEnum, slotStart, slotEnd, localNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(existingPendingDonation.Id);
        }

        var hasAppAccount = donor.User?.PasswordHash != null;
        var appointment = DonationAppointment.RegisterSystemDonation(
            donorId: donor.Id,
            donationCenterId: request.DonationCenterId,
            donationType: DonationType.WholeBlood,
            source: sourceEnum,
            isNewDonor: isNewDonor,
            hasAppAccount: hasAppAccount,
            slotStart: slotStart,
            slotEnd: slotEnd,
            currentLocalTime: localNow
        );

        var fortyEightHoursAgo = dateTimeProvider.UtcNow.AddDays(-2);

        var latestEmergencyNotification = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == donor.Id &&
                        n.Type == NotificationType.UrgentBloodAppeal &&
                        n.SentDate >= fortyEightHoursAgo)
            .OrderByDescending(n => n.SentDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestEmergencyNotification != null && !string.IsNullOrEmpty(latestEmergencyNotification.ActionPayload))
        {
            try
            {
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(latestEmergencyNotification.ActionPayload);
                if (jsonDoc.RootElement.TryGetProperty("targetId", out var targetIdProp) &&
                    Guid.TryParse(targetIdProp.GetString(), out var appealId))
                {
                    var isActiveAppeal = await dbContext.UrgentBloodAppeals
                        .AnyAsync(uba => uba.Id == appealId && uba.IsActive, cancellationToken);

                    if (isActiveAppeal)
                    {
                        appointment.SetUrgentBloodAppeal(appealId);
                    }
                }
            }
            catch
            {
                // Ignore JSON parsing errors and proceed
            }
        }

        await dbContext.DonationAppointments.AddAsync(appointment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(appointment.Id);
    }

    private async Task<Result<bool>> CheckDonorEligibilityInternalAsync(Guid donorId, CancellationToken cancellationToken)
    {
        var eligibility = await eligibilityService.CheckEligibilityAsync(donorId, DonationType.WholeBlood, cancellationToken);

        if (!eligibility.IsSuccess)
        {
            var errorMsg = eligibility.Error == "Donor not found." ? "المتبرع غير موجود." : eligibility.Error;
            return Result<bool>.Failure(errorMsg!);
        }

        if (!eligibility.Data!.IsEligible)
        {
            string rejectionReasonAr = eligibility.Data switch
            {
                { CooldownRemainingDays: not null } => 
                    $"يجب على المتبرع الانتظار {eligibility.Data.CooldownRemainingDays.Value} يومًا إضافيًا قبل التبرع مرة أخرى.",
                { DeferredUntil: not null } => 
                    $"المتبرع مستبعد مؤقتًا من التبرع حتى {eligibility.Data.DeferredUntil.Value:yyyy-MM-dd}.",
                _ => 
                    "المتبرع غير مؤهل للتبرع بشكل دائم."
            };

            return Result<bool>.Failure(rejectionReasonAr);
        }

        return Result<bool>.Success(true);
    }

    private async Task<Result<Donor>> CreateNewDonorAsync(
        CreateDonationCommand request,
        string nationalIdClean,
        string phoneClean,
        string[] nameParts,
        DateOnly dateOfBirth,
        Gender gender,
        string address,
        CancellationToken cancellationToken)
    {
        // Check if User already exists under this National ID
        var user = await userManager.FindByNameAsync(nationalIdClean);
        if (user == null)
        {
            user = new User
            {
                UserName = nationalIdClean,
                PhoneNumber = phoneClean,
                PhoneNumberConfirmed = false
            };

            var userResult = await userManager.CreateAsync(user);
            if (!userResult.Succeeded)
            {
                var errors = string.Join(", ", userResult.Errors.Select(e => e.Description));
                return Result<Donor>.Failure(errors);
            }
        }

        // Ensure "Donor" role exists and is assigned
        if (!await roleManager.RoleExistsAsync("Donor"))
        {
            await roleManager.CreateAsync(new Role { Name = "Donor" });
        }
        if (!await userManager.IsInRoleAsync(user, "Donor"))
        {
            await userManager.AddToRoleAsync(user, "Donor");
        }

        var donor = new Donor
        {
            Id = user.Id,
            FirstName = nameParts[0],
            SecondName = nameParts[1],
            ThirdName = nameParts[2],
            FourthName = nameParts.Length > 3 ? nameParts[3] : null,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            PhoneNumber = phoneClean,
            NationalId = nationalIdClean,
            Governorate = request.Governorate.Trim(),
            District = request.District.Trim(),
            Area = request.Area?.Trim(),
            Address = address,
            Status = DonorStatus.Eligible,
            IsRegistrationCompleted = false
        };

        await dbContext.Donors.AddAsync(donor, cancellationToken);
        // Save immediately so Donor is present in DB for FK constraints on Appointment booking
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Donor>.Success(donor);
    }

    private void UpdateExistingDonor(
        Donor donor,
        CreateDonationCommand request,
        string phoneClean,
        string[] nameParts,
        DateOnly dateOfBirth,
        Gender gender,
        string address)
    {
        donor.FirstName = nameParts[0];
        donor.SecondName = nameParts[1];
        donor.ThirdName = nameParts[2];
        donor.FourthName = nameParts.Length > 3 ? nameParts[3] : null;
        donor.DateOfBirth = dateOfBirth;
        donor.Gender = gender;
        donor.PhoneNumber = phoneClean;
        donor.Governorate = request.Governorate.Trim();
        donor.District = request.District.Trim();
        donor.Area = request.Area?.Trim();
        donor.Address = address;

        if (donor.User != null)
        {
            donor.User.PhoneNumber = phoneClean;
        }
    }

    private Result<(TimeSpan Start, TimeSpan End)> ResolveTimeSlot(
        DonationCenter center,
        DonationAppointment? existingPendingDonation,
        DateTime localNow,
        DonationSource source)
    {
        if (existingPendingDonation != null && existingPendingDonation.ScheduledDate.Date == localNow.Date)
        {
            return Result<(TimeSpan, TimeSpan)>.Success((existingPendingDonation.StartTime, existingPendingDonation.EndTime));
        }

        var time = localNow.TimeOfDay;

        // For all walk-in donations (from the doctor system): the doctor is physically at the center with the donor,
        // so we always assign a slot at the current time regardless of opening hours/schedules/day of week/exclusions.
        if (source == DonationSource.WalkIn)
        {
            var slotDuration = center.SlotDurationMinutes ?? 15;
            var slotEnd = TimeSpan.FromTicks(time.Add(TimeSpan.FromMinutes(slotDuration)).Ticks % TimeSpan.TicksPerDay);
            return Result<(TimeSpan, TimeSpan)>.Success((time, slotEnd));
        }

        var slots = center.GenerateTimeSlotsForDate(localNow, center.CenterExclusions.ToList(), center.OpeningHours.ToList());

        // Use a direct typed search — Cast<nullable_tuple>() does not work on non-nullable value-tuple lists
        (TimeSpan Start, TimeSpan End, int MaxPerSlot)? matchingSlot = null;
        foreach (var slot in slots)
        {
            if (IsTimeInSlot(time, slot.Start, slot.End))
            {
                matchingSlot = slot;
                break;
            }
        }

        if (matchingSlot.HasValue)
        {
            return Result<(TimeSpan, TimeSpan)>.Success((matchingSlot.Value.Start, matchingSlot.Value.End));
        }

        if (center.CenterType == CenterType.Campaign)
        {
            var bufferOpen = TimeSpan.FromTicks(center.StartTime.Add(TimeSpan.FromMinutes(-30)).Ticks % TimeSpan.TicksPerDay);
            if (bufferOpen < TimeSpan.Zero) bufferOpen = bufferOpen.Add(TimeSpan.FromDays(1));

            var bufferClose = TimeSpan.FromTicks(center.EndTime.Add(TimeSpan.FromMinutes(30)).Ticks % TimeSpan.TicksPerDay);
            if (bufferClose < TimeSpan.Zero) bufferClose = bufferClose.Add(TimeSpan.FromDays(1));

            bool inBuffer;
            if (bufferOpen <= bufferClose)
            {
                inBuffer = time >= bufferOpen && time <= bufferClose;
            }
            else
            {
                inBuffer = time >= bufferOpen || time <= bufferClose;
            }
            
            if (inBuffer)
            {
                var slotDuration = center.SlotDurationMinutes ?? 15;
                var slotEnd = TimeSpan.FromTicks(time.Add(TimeSpan.FromMinutes(slotDuration)).Ticks % TimeSpan.TicksPerDay);
                return Result<(TimeSpan, TimeSpan)>.Success((time, slotEnd));
            }

            return Result<(TimeSpan, TimeSpan)>.Failure("The campaign is currently closed or has no available slots at this time.");
        }

        // Fallback for non-walk-in requests
        {
            var slotDuration = center.SlotDurationMinutes ?? 15;
            var slotEnd = TimeSpan.FromTicks(time.Add(TimeSpan.FromMinutes(slotDuration)).Ticks % TimeSpan.TicksPerDay);
            return Result<(TimeSpan, TimeSpan)>.Success((time, slotEnd));
        }
    }

    private static bool IsTimeInSlot(TimeSpan time, TimeSpan start, TimeSpan end)
    {
        if (start <= end)
        {
            return time >= start && time < end;
        }
        else
        {
            return time >= start || time < end;
        }
    }
}
