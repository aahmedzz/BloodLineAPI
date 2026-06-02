using System;

namespace BloodLineAPI.Application.Features.Appointments.Dtos;

public sealed record SystemAppointmentDetailsDto(
    Guid Id,
    string Date,
    string Time,
    string Status,
    string DonorName,
    string DonorCode,
    string DonorNationalId,
    string DonorPhone,
    string? DonorBloodType,
    string DonorGender,
    int DonorAge,
    string DonorDateOfBirth,
    string? DonorGovernorate,
    string? DonorDistrict,
    string? DonorArea,
    string DonationType,
    Guid CenterId,
    string CenterType,
    string? Notes = null,
    string? CompletedAt = null,
    string? CancelledAt = null,
    string? CancellationReason = null
);
