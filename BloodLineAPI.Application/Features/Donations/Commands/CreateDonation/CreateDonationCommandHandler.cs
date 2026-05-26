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
    IDonorEligibilityService eligibilityService)
    : IRequestHandler<CreateDonationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateDonationCommand request,
        CancellationToken cancellationToken)
    {
        var nationalIdClean = request.NationalId.Trim();
        var phoneClean = request.Phone.Trim();

        // 1. Check if Donor already exists
        var donor = await dbContext.Donors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.NationalId == nationalIdClean, cancellationToken);

        DonationAppointment? existingPendingDonation = null;
        if (donor != null)
        {
            var activeDonation = await dbContext.DonationAppointments
                .FirstOrDefaultAsync(da => da.DonorId == donor.Id && 
                                          (da.DonationStatus == DonationStatus.Pending || da.DonationStatus == DonationStatus.Approved), 
                                     cancellationToken);

            if (activeDonation != null)
            {
                if (activeDonation.DonationStatus == DonationStatus.Pending)
                {
                    existingPendingDonation = activeDonation;
                }
                else
                {
                    return Result<Guid>.Failure("هذا المتبرع لديه بالفعل تبرع معتمد قيد التنفيذ.");
                }
            }
            else
            {
                var eligibility = await eligibilityService.CheckEligibilityAsync(
                    donor.Id, DonationType.WholeBlood, cancellationToken);

                if (!eligibility.IsSuccess)
                {
                    var errorMsg = eligibility.Error == "Donor not found." ? "المتبرع غير موجود." : eligibility.Error;
                    return Result<Guid>.Failure(errorMsg!);
                }

                if (!eligibility.Data!.IsEligible)
                {
                    string rejectionReasonAr;
                    if (eligibility.Data.CooldownRemainingDays.HasValue)
                    {
                        rejectionReasonAr = $"يجب على المتبرع الانتظار {eligibility.Data.CooldownRemainingDays.Value} يومًا إضافيًا قبل التبرع مرة أخرى.";
                    }
                    else if (eligibility.Data.DeferredUntil.HasValue)
                    {
                        rejectionReasonAr = $"المتبرع مستبعد مؤقتًا من التبرع حتى {eligibility.Data.DeferredUntil.Value:yyyy-MM-dd}.";
                    }
                    else
                    {
                        rejectionReasonAr = "المتبرع غير مؤهل للتبرع بشكل دائم.";
                    }

                    return Result<Guid>.Failure(rejectionReasonAr);
                }
            }
        }

        bool isNewDonor = false;
        bool hasAppAccount = false;

        if (donor == null)
        {
            isNewDonor = true;
            hasAppAccount = false;

            // Check if User already exists under this National ID or Phone (e.g. registered by other staff roles)
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
                    return Result<Guid>.Failure(errors);
                }
            }

            // Assign "Donor" role to user
            if (!await roleManager.RoleExistsAsync("Donor"))
            {
                await roleManager.CreateAsync(new Role { Name = "Donor" });
            }
            if (!await userManager.IsInRoleAsync(user, "Donor"))
            {
                await userManager.AddToRoleAsync(user, "Donor");
            }

            // Split name
            var nameParts = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (nameParts.Length < 3)
            {
                return Result<Guid>.Failure("Full name must contain at least 3 names.");
            }

            donor = new Donor
            {
                Id = user.Id,
                FirstName = nameParts[0],
                SecondName = nameParts[1],
                ThirdName = nameParts[2],
                FourthName = nameParts.Length > 3 ? nameParts[3] : null,
                DateOfBirth = DateOnly.Parse(request.DateOfBirth),
                Gender = request.Gender.Trim().Equals("male", StringComparison.OrdinalIgnoreCase) ? Gender.Male : Gender.Female,
                PhoneNumber = phoneClean,
                NationalId = nationalIdClean,
                Governorate = request.Governorate.Trim(),
                District = request.District.Trim(),
                Area = request.Area?.Trim(),
                Status = DonorStatus.Eligible,
                IsRegistrationCompleted = false
            };

            // Set address property
            var addressParts = new[] { donor.Governorate, donor.District, donor.Area }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            donor.Address = string.Join(", ", addressParts);

            await dbContext.Donors.AddAsync(donor, cancellationToken);
            // Save immediately to ensure Donor exists in Db for FK constraint on Appointment creation
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Donor already exists - Update their profile with the latest information sent in Step 1
            isNewDonor = false;
            hasAppAccount = donor.User != null && donor.User.PasswordHash != null;

            var nameParts = request.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (nameParts.Length < 3)
            {
                return Result<Guid>.Failure("Full name must contain at least 3 names.");
            }

            donor.FirstName = nameParts[0];
            donor.SecondName = nameParts[1];
            donor.ThirdName = nameParts[2];
            donor.FourthName = nameParts.Length > 3 ? nameParts[3] : null;
            donor.DateOfBirth = DateOnly.Parse(request.DateOfBirth);
            donor.Gender = request.Gender.Trim().Equals("male", StringComparison.OrdinalIgnoreCase) ? Gender.Male : Gender.Female;
            donor.PhoneNumber = phoneClean;
            donor.Governorate = request.Governorate.Trim();
            donor.District = request.District.Trim();
            donor.Area = request.Area?.Trim();

            var addressParts = new[] { donor.Governorate, donor.District, donor.Area }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            donor.Address = string.Join(", ", addressParts);

            if (donor.User != null)
            {
                donor.User.PhoneNumber = phoneClean;
            }
        }

        var sourceEnum = request.Source.Trim().ToLowerInvariant() switch
        {
            "campaign" => DonationSource.Campaign,
            "mobileapp" => DonationSource.MobileApp,
            _ => DonationSource.WalkIn
        };

        if (existingPendingDonation != null)
        {
            existingPendingDonation.UpdateSystemDonation(request.DonationCenterId, sourceEnum);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(existingPendingDonation.Id);
        }

        // Donation type is now selected in step 2, so we create the initial appointment with a default value.
        var donationTypeEnum = DonationType.WholeBlood;

        // 2. Register the System Donation Appointment
        var appointment = DonationAppointment.RegisterSystemDonation(
            donorId: donor.Id,
            donationCenterId: request.DonationCenterId,
            donationType: donationTypeEnum,
            source: sourceEnum,
            isNewDonor: isNewDonor,
            hasAppAccount: hasAppAccount
        );

        await dbContext.DonationAppointments.AddAsync(appointment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(appointment.Id);
    }
}
