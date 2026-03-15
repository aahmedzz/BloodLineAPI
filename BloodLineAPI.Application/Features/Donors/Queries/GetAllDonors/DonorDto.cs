using BloodBankSystem.Domain.Enums;

namespace BloodLineAPI.Application.Features.Donors.Queries.GetAllDonors;

public sealed record DonorDto(
    Guid Id,
    string FullName,
    DateOnly DateOfBirth,
    BloodType BloodType,
    string PhoneNumber);
