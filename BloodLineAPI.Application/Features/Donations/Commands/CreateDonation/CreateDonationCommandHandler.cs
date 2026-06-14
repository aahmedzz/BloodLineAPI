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
            // Check for any active donation at the requested center (Pending or Approved)
            var activeDonation = await dbContext.DonationAppointments
                .FirstOrDefaultAsync(da => da.DonorId == donor.Id && 
                                          da.DonationCenterId == request.DonationCenterId &&
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
            else
            {
                // Verify eligibility for existing donor
                var eligibilityResult = await CheckDonorEligibilityInternalAsync(donor.Id, cancellationToken);
                if (!eligibilityResult.IsSuccess)
                {
                    return Result<Guid>.Failure(eligibilityResult.Error!);
                }
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

        var localNow = dateTimeProvider.LocalNow;
        if (!center.IsOperatingOn(localNow))
        {
            return Result<Guid>.Failure("Donation center is closed today.");
        }

        // 4. Resolve Time Slot
        var slotResult = ResolveTimeSlot(center, existingPendingDonation, localNow);
        if (!slotResult.IsSuccess)
        {
            return Result<Guid>.Failure(slotResult.Error!);
        }

        var (slotStart, slotEnd) = slotResult.Data;
        var sourceEnum = request.Source.Trim().ToLowerInvariant() switch
        {
            "campaign" => DonationSource.Campaign,
            "mobileapp" => DonationSource.MobileApp,
            _ => DonationSource.WalkIn
        };

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
        DateTime localNow)
    {
        if (existingPendingDonation != null && existingPendingDonation.ScheduledDate.Date == localNow.Date)
        {
            return Result<(TimeSpan, TimeSpan)>.Success((existingPendingDonation.StartTime, existingPendingDonation.EndTime));
        }

        var slots = center.GenerateTimeSlotsForDate(localNow, center.CenterExclusions.ToList(), center.OpeningHours.ToList());
        var time = localNow.TimeOfDay;
        var matchingSlot = slots.Cast<(TimeSpan Start, TimeSpan End, int MaxPerSlot)?>()
            .FirstOrDefault(s => s.HasValue && IsTimeInSlot(time, s.Value.Start, s.Value.End));

        if (matchingSlot != null)
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

        return Result<(TimeSpan, TimeSpan)>.Failure("The center is currently closed or has no available slots at this time.");
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
