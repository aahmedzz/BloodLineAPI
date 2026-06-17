using System;

namespace BloodLineAPI.Application.Features.Appointments.Dtos;

public sealed record SystemAppointmentSlotDto(
    string Id,
    string Date,
    string Time,
    string Status,
    string? DonorName = null,
    string? DonorCode = null,
    string? DonorNationalId = null,
    string? DonorPhone = null,
    string? DonorBloodType = null,
    string? DonorGender = null,
    int? DonorAge = null,
    string? DonorDistrict = null,
    string? DonorArea = null,
    string? DonationType = null,
    Guid? CampaignId = null,
    string? Notes = null,
    string? CompletedAt = null,
    string? CancelledAt = null,
    string? CancelledBy = null,
    string? CancelledByName = null,
    string? CancellationReason = null
);
