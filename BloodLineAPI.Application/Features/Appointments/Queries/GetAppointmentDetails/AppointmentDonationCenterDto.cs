namespace BloodLineAPI.Application.Features.Appointments.Queries.GetAppointmentDetails;

public sealed record AppointmentDonationCenterDto(
    Guid Id,
    string Name,
    string Location,
    string AddressDetails,
    double Latitude,
    double Longitude,
    string CenterType,
    string Status,
    string OperatingHours);
