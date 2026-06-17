using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Application.Common.Interfaces;

/// <summary>
/// Centralised donor eligibility checks that can be reused across
/// system-side (doctor portal), mobile app, and campaign donation flows.
/// </summary>
public interface IDonorEligibilityService
{
    /// <summary>
    /// Performs all eligibility checks for a donor and returns a result indicating
    /// whether the donor is eligible to donate, along with a reason if not.
    /// </summary>
    /// <param name="donorId">The donor's ID.</param>
    /// <param name="donationType">The type of donation being requested.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success if eligible, Failure with the reason if not.</returns>
    Task<Result<DonorEligibilityResult>> CheckEligibilityAsync(
        Guid donorId,
        DonationType donationType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contains the details of an eligibility check result.
/// </summary>
public record DonorEligibilityResult(
    bool IsEligible,
    DateTime? DeferredUntil = null,
    string? RejectionReason = null,
    int? CooldownRemainingDays = null);
