namespace BloodLineAPI.Application.Common.Models;

public record MobileUserProfileResponse(
    // ── Home Screen ──────────────────────────────
    Guid    UserId,
    string  FullName,
    string? BloodType,              // "A+", "O-", etc.
    int     TotalPoints,
    DonorBadgeDto? LastBadge,       // most recently earned badge, null if none

    // ── Profile Screen — Identity (read-only) ────
    string  NationalId,
    string  Gender,                 // "male" / "female"
    string  DateOfBirth,            // "yyyy-MM-dd"
    int     Age,
    string  PhoneNumber,
    string  DonorCode,

    // ── Profile Screen — Medical ─────────────────
    decimal? WeightKg,              // editable

    // ── Profile Screen — Address (all editable) ──
    string? Governorate,
    string? District,
    string? Area,

    // ── Profile Screen — Donation Stats ──────────
    int     TotalDonationCount,
    string? LastDonationDate,       // "yyyy-MM-dd" or null
    string  Status,                 // "eligible" / "cooldown" / "ineligible"
    string? DeferredUntil,          // "yyyy-MM-dd" or null

    // ── Auth State ───────────────────────────────
    bool IsPhoneNumberVerified,
    bool IsRegistrationCompleted,

    double? Latitude = null,
    double? Longitude = null
);

public record DonorBadgeDto(
    string BadgeKey,
    string BadgeName,
    string BadgeNameAr,
    string BadgeDescription,
    string IconUrl,
    string BadgeType,
    int    BonusPoints,
    string EarnedAt                 // "yyyy-MM-dd"
);
