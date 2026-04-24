namespace BloodLineAPI.Application.Features.Appointments.Dtos;

public sealed record DonationCenterDto(
    Guid Id,
    string Name,
    string Location,
    string AddressDetails,
    double Latitude,
    double Longitude,
    string CenterType,
    string Status,
    string OperatingHours,
    IReadOnlyList<string> AvailableDonationTypes);
