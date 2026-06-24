namespace BloodLineAPI.Application.Common.Models;

public record MyEligibilityResponse(
    string  Status,                  // "eligible" | "cooldown" | "ineligible"
    bool    IsEligible,
    string? LastDonationDate,        // "yyyy-MM-dd" or null (first-time donor)
    string? NextEligibleDate,        // "yyyy-MM-dd" or null if already eligible / permanently ineligible
    int?    CooldownRemainingDays,   // null if eligible or permanently ineligible
    int     TotalCooldownDays,       // e.g. 90 for male whole blood — for animation denominator
    double  RecoveryPercent,         // 0.0 – 100.0 — drives the body fill animation directly
    string? DeferredUntil,           // "yyyy-MM-dd" or null
    string? DeferralReason           // clinical reason string or null
);
