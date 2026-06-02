using System;
using System.Linq;
using BloodLineAPI.Domain.Entities;

namespace BloodLineAPI.Application.Features.Donors.Queries.GetFilteredDonors;

public record FilteredDonorDto(
    Guid Id,
    string DonorCode,
    string Name,
    string NationalId,
    string Gender,
    string DateOfBirth,
    int Age,
    string Phone,
    string BloodType,
    string Status,
    string Address,
    string Governorate,
    string District,
    string Area,
    string? DeferredUntil,
    string? RejectionReason,
    bool HasAppAccount,
    string RegisteredAt,
    string? LastDonationDate,
    int Donations,
    int Points)
{
    public static FilteredDonorDto MapFrom(
        Donor donor,
        MedicalScreening? latestScreening,
        DateOnly today)
    {
        // Age calculation
        var age = today.Year - donor.DateOfBirth.Year;
        if (donor.DateOfBirth > today.AddYears(-age)) age--;

        // Address string compilation
        var addressParts = new[] { donor.Governorate, donor.District, donor.Area }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var fullAddress = addressParts.Any() ? string.Join(", ", addressParts) : (donor.Address ?? string.Empty);

        var deferredUntilStr = latestScreening?.LockoutUntil?.ToString("yyyy-MM-dd");
        var rejectionReason = latestScreening?.RejectionReason;

        var bloodTypeDisplay = donor.BloodType?.FullDisplayname ?? string.Empty;
        var hasAppAccount = donor.User != null && donor.User.PasswordHash != null;

        return new FilteredDonorDto(
            Id: donor.Id,
            DonorCode: donor.DonorCode,
            Name: donor.FullName,
            NationalId: donor.NationalId,
            Gender: donor.Gender.ToString().ToLowerInvariant(),
            DateOfBirth: donor.DateOfBirth.ToString("yyyy-MM-dd"),
            Age: age,
            Phone: donor.PhoneNumber,
            BloodType: bloodTypeDisplay,
            Status: donor.Status.ToString().ToLowerInvariant(),
            Address: fullAddress,
            Governorate: donor.Governorate ?? string.Empty,
            District: donor.District ?? string.Empty,
            Area: donor.Area ?? string.Empty,
            DeferredUntil: deferredUntilStr,
            RejectionReason: rejectionReason,
            HasAppAccount: hasAppAccount,
            RegisteredAt: donor.CreatedAt.ToString("yyyy-MM-dd"),
            LastDonationDate: donor.LastDonationDate?.ToString("yyyy-MM-dd"),
            Donations: donor.TotalDonationCount,
            Points: donor.TotalPoints
        );
    }
}
