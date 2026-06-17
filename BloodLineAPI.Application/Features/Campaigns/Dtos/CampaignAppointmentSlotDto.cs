namespace BloodLineAPI.Application.Features.Campaigns.Dtos;

public record CampaignAppointmentSlotDto(
    string Id,
    string Date,                  // "YYYY-MM-DD"
    string Time,                  // "HH:mm"
    string Status,                // "booked" | "completed" | "missed" | "cancelled"
    string? DonorName = null,
    string? DonorCode = null,
    string? DonorNationalId = null,
    string? DonorPhone = null,
    string? DonorBloodType = null, // e.g. "A+", "O-"
    string? DonorGender = null,    // "male" | "female"
    int? DonorAge = null,
    string? DonationType = null,   // "wholeblood" | "plasma" | "platelets"
    string? CampaignId = null,     // CAM-XXX
    string? Notes = null,
    string? CompletedAt = null,    // "HH:mm" or ISO
    string? CancelledAt = null,
    string? CancelledBy = null,
    string? CancelledByName = null,
    string? CancellationReason = null
);
