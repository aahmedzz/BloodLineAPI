using System;

namespace BloodLineAPI.Application.Features.Campaigns.Dtos;

public record CampaignDto(
    string Id,                  // Database Guid Id as string
    string CampaignCode,        // CAM-XXX (derived from CampaignCode)
    string Title,               // DonationCenter Name
    string City,                // DonationCenter Location
    double? Latitude,
    double? Longitude,
    string Date,                // "YYYY-MM-DD"
    string StartTime,           // "HH:mm"
    string EndTime,             // "HH:mm"
    int SlotDuration,           // minutes
    int SlotCapacity,           // max donors per slot
    int TargetDonors,
    int RegisteredDonors,       // Count of bookings on this date (non-cancelled)
    int AppointmentsCount,      // Count of bookings on this date via MobileApp
    string Status,              // "active" | "notactive" | "completed"
    string CreatedBy,           // User ID (Guid string)
    string CreatedByName,       // Creator Full Name
    string Description,         // Description text
    RecurrenceSettingsDto? Recurrence,
    IReadOnlyList<string> AvailableDonationTypes
);
