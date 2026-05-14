using System.ComponentModel;
using System.Text;
using BloodLineAPI.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Security.Claims;

namespace BloodLineAPI.Infrastructure.Chatbot.Plugins;

/// <summary>
/// Personalized plugin that queries the authenticated donor's own data:
/// blood type info, lab results, donation history, next eligibility date, and medical advice.
/// The donor ID is injected per-request via Kernel.Data["donorId"].
/// </summary>
public class DonorProfilePlugin
{
    private const string NotLoggedInMessage = "I couldn't identify your account. Please make sure you are logged in.";
    private const string ProfileNotFoundMessage = "I couldn't find your donor profile.";

    private readonly IApplicationDbContext _context;
    private readonly ILogger<DonorProfilePlugin> _logger;
    private readonly IMemoryCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DonorProfilePlugin(
        IApplicationDbContext context,
        ILogger<DonorProfilePlugin> logger,
        IMemoryCache cache,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    [KernelFunction, Description("Gets the current donor's blood type information and what it means for compatibility. Useful when the user asks 'what is my blood type?', 'who can I donate to?', or 'who can donate to me?'.")]
    public async Task<string> GetMyBloodTypeInfoAsync()
    {
        return await ExecuteForDonorAsync("blood type info", async (donorId, ct) =>
        {
            var donor = await _context.Donors
                .AsNoTracking()
                .Include(d => d.BloodType)
                .Where(d => d.Id == donorId)
                .Select(d => new
                {
                    d.FirstName,
                    d.BloodTypeId,
                    BloodGroupName = d.BloodType != null ? d.BloodType.BloodGroupName.ToString() : null,
                    RhFactor = d.BloodType != null ? d.BloodType.RhFactor : (BloodLineAPI.Domain.Enums.RhFactor?)null
                })
                .FirstOrDefaultAsync(ct);

            if (donor == null)
                return ProfileNotFoundMessage;

            if (donor.BloodTypeId == null || donor.BloodGroupName == null)
                return $"Hi {donor.FirstName}, your blood type has not been determined yet. It will be confirmed after your first donation and lab testing.";

            var rhSign = donor.RhFactor == BloodLineAPI.Domain.Enums.RhFactor.Positive ? "+" : "-";
            var fullType = $"{donor.BloodGroupName}{rhSign}";

            var sb = new StringBuilder();
            sb.AppendLine($"Your blood type is: {fullType}");
            sb.AppendLine();
            sb.AppendLine(GetCompatibilityInfo(donor.BloodGroupName, rhSign));

            return sb.ToString();
        });
    }

    [KernelFunction, Description("Gets the donor's latest blood test / lab results and highlights any issues. Useful when the user asks 'what are my lab results?', 'are my test results safe?', 'do I have any health issues?', or 'my blood test'.")]
    public async Task<string> GetMyLatestLabResultsAsync()
    {
        return await ExecuteForDonorAsync("lab results", async (donorId, ct) =>
        {
            // Lab results are linked through: Donor -> DonationAppointment -> BloodBag -> BloodTestResult
            var latestResult = await _context.BloodTestResults
                .AsNoTracking()
                .Include(r => r.ConfirmedBloodType)
                .Where(r => r.BloodBag.DonationAppointment != null
                         && r.BloodBag.DonationAppointment.DonorId == donorId)
                .OrderByDescending(r => r.TestDate)
                .Select(r => new
                {
                    r.TestDate,
                    r.IsSafe,
                    r.HepatitisBResult,
                    r.HepatitisCResult,
                    r.HivResult,
                    r.SyphilisResult,
                    r.Notes,
                    BloodType = r.ConfirmedBloodType != null
                        ? r.ConfirmedBloodType.BloodGroupName.ToString() + (r.ConfirmedBloodType.RhFactor == BloodLineAPI.Domain.Enums.RhFactor.Positive ? "+" : "-")
                        : null
                })
                .FirstOrDefaultAsync(ct);

            if (latestResult == null)
                return "You don't have any lab test results yet. Results become available after your donated blood is tested at the center.";

            var sb = new StringBuilder();
            sb.AppendLine($"Your Latest Lab Results (tested on {latestResult.TestDate:yyyy-MM-dd}):");
            sb.AppendLine();
            sb.AppendLine($"Overall Status: {(latestResult.IsSafe ? "✅ SAFE — All clear!" : "⚠️ FLAGGED — One or more tests need attention.")}");
            sb.AppendLine();
            sb.AppendLine("Test Breakdown:");
            sb.AppendLine($"  - Hepatitis B: {FormatTestResult(latestResult.HepatitisBResult)}");
            sb.AppendLine($"  - Hepatitis C: {FormatTestResult(latestResult.HepatitisCResult)}");
            sb.AppendLine($"  - HIV: {FormatTestResult(latestResult.HivResult)}");
            sb.AppendLine($"  - Syphilis: {FormatTestResult(latestResult.SyphilisResult)}");

            if (latestResult.BloodType != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Confirmed Blood Type: {latestResult.BloodType}");
            }

            if (!string.IsNullOrWhiteSpace(latestResult.Notes))
            {
                sb.AppendLine();
                sb.AppendLine($"Doctor's Notes: {latestResult.Notes}");
            }

            if (!latestResult.IsSafe)
            {
                sb.AppendLine();
                sb.AppendLine("⚠️ IMPORTANT: Your results indicate a potential issue. Please:");
                sb.AppendLine("  1. Visit the nearest hospital or clinic for a confirmatory test.");
                sb.AppendLine("  2. Consult a specialist doctor as soon as possible.");
                sb.AppendLine("  3. Do not attempt to donate blood until you have been cleared by a medical professional.");
                sb.AppendLine("  4. Contact the Monqez main branch for guidance and support.");
            }

            return sb.ToString();
        });
    }

    [KernelFunction, Description("Gets the donor's donation history summary and when they can next donate. Useful when the user asks 'when can I donate again?', 'how many times have I donated?', 'my donation history', or 'next donation date'.")]
    public async Task<string> GetMyDonationSummaryAsync()
    {
        return await ExecuteForDonorAsync("donation summary", async (donorId, ct) =>
        {
            var donor = await _context.Donors
                .AsNoTracking()
                .Where(d => d.Id == donorId)
                .Select(d => new
                {
                    d.FirstName,
                    d.TotalDonationCount,
                    d.LastDonationDate,
                    d.TotalPoints,
                    d.Gender
                })
                .FirstOrDefaultAsync(ct);

            if (donor == null)
                return ProfileNotFoundMessage;

            var sb = new StringBuilder();
            sb.AppendLine($"Donation Summary for {donor.FirstName}:");
            sb.AppendLine();
            sb.AppendLine($"Total Donations: {donor.TotalDonationCount}");
            sb.AppendLine($"Reward Points: {donor.TotalPoints}");

            if (donor.LastDonationDate.HasValue)
            {
                sb.AppendLine($"Last Donation: {donor.LastDonationDate.Value:yyyy-MM-dd}");

                // Cooldown: males 3 months (90 days), females 4 months (120 days)
                var cooldownDays = donor.Gender == BloodLineAPI.Domain.Enums.Gender.Female ? 120 : 90;
                var nextEligible = donor.LastDonationDate.Value.AddDays(cooldownDays);

                if (nextEligible > DateTime.UtcNow)
                {
                    var daysLeft = (nextEligible - DateTime.UtcNow).Days;
                    sb.AppendLine($"Next Eligible Date: {nextEligible:yyyy-MM-dd} ({daysLeft} days remaining)");
                }
                else
                {
                    sb.AppendLine("You are eligible to donate now! 🎉");
                }
            }
            else
            {
                sb.AppendLine("You haven't donated yet. You are welcome to book your first appointment!");
            }

            return sb.ToString();
        });
    }

    [KernelFunction, Description("Gets the donor's latest medical screening result from the center visit. Useful when the user asks 'what was my medical screening result?', 'was I eligible last time?', 'why was I rejected?', or 'am I locked out?'.")]
    public async Task<string> GetMyLatestMedicalScreeningAsync()
    {
        return await ExecuteForDonorAsync("medical screening", async (donorId, ct) =>
        {
            var screening = await _context.MedicalScreenings
                .AsNoTracking()
                .Where(s => s.DonorId == donorId)
                .OrderByDescending(s => s.ScreeningDate)
                .Select(s => new
                {
                    s.ScreeningDate,
                    s.IsEligible,
                    s.Weight,
                    s.HemoglobinLevel,
                    s.SystolicBP,
                    s.DiastolicBP,
                    s.Temperature,
                    s.PulseRate,
                    s.HasChronicDiseases,
                    s.ChronicDiseaseDetails,
                    s.IsAllergic,
                    s.RejectionReason,
                    s.LockoutUntil
                })
                .FirstOrDefaultAsync(ct);

            if (screening == null)
                return "You don't have any medical screening records yet. A screening is performed when you visit the donation center.";

            var sb = new StringBuilder();
            sb.AppendLine($"Latest Medical Screening ({screening.ScreeningDate:yyyy-MM-dd}):");
            sb.AppendLine();
            sb.AppendLine($"Result: {(screening.IsEligible ? "✅ Eligible to donate" : "❌ Not eligible")}");
            sb.AppendLine();
            sb.AppendLine("Vitals:");
            sb.AppendLine($"  - Weight: {screening.Weight} kg");
            sb.AppendLine($"  - Hemoglobin: {screening.HemoglobinLevel} g/dL");
            sb.AppendLine($"  - Blood Pressure: {screening.SystolicBP}/{screening.DiastolicBP} mmHg");
            sb.AppendLine($"  - Temperature: {screening.Temperature} °C");
            sb.AppendLine($"  - Pulse Rate: {screening.PulseRate} bpm");

            if (screening.HasChronicDiseases && !string.IsNullOrWhiteSpace(screening.ChronicDiseaseDetails))
            {
                sb.AppendLine();
                sb.AppendLine($"Chronic Conditions Noted: {screening.ChronicDiseaseDetails}");
            }

            if (!screening.IsEligible && !string.IsNullOrWhiteSpace(screening.RejectionReason))
            {
                sb.AppendLine();
                sb.AppendLine($"Rejection Reason: {screening.RejectionReason}");
                sb.AppendLine();
                sb.AppendLine("Advice:");
                sb.AppendLine("  - Please consult your doctor regarding the rejection reason.");
                sb.AppendLine("  - Address any underlying health issues before attempting to donate again.");
                sb.AppendLine("  - You can contact the Monqez main branch for more information.");
            }

            if (screening.LockoutUntil.HasValue && screening.LockoutUntil.Value > DateTime.UtcNow)
            {
                sb.AppendLine();
                sb.AppendLine($"⚠️ You are temporarily locked out from donating until {screening.LockoutUntil.Value:yyyy-MM-dd}.");
                sb.AppendLine("This lockout was applied due to a failed medical screening. Please consult with a medical professional before your next attempt.");
            }

            return sb.ToString();
        });
    }

    // --- Helpers ---

    /// <summary>
    /// Centralizes the donor ID extraction, timeout, and error handling boilerplate.
    /// Every kernel function delegates to this method with its specific query logic.
    /// </summary>
    private async Task<string> ExecuteForDonorAsync(
        string operationName,
        Func<Guid, CancellationToken, Task<string>> action)
    {
        var donorId = GetDonorId();
        if (donorId == Guid.Empty)
            return NotLoggedInMessage;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            return await action(donorId, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Operation} for donor {DonorId}.", operationName, donorId);
            return $"Unable to fetch your {operationName} at this time. Please try again later.";
        }
    }

    private Guid GetDonorId()
    {
        var idStr = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(idStr) && Guid.TryParse(idStr, out var id))
            return id;

        return Guid.Empty;
    }

    private static string FormatTestResult(string? result)
    {
        if (string.IsNullOrWhiteSpace(result))
            return "Not tested";

        return result.Equals("Negative", StringComparison.OrdinalIgnoreCase)
            ? "Negative ✅"
            : $"{result} ⚠️";
    }

    private static string GetCompatibilityInfo(string bloodGroup, string rh)
    {
        var fullType = $"{bloodGroup}{rh}";
        return fullType switch
        {
            "O-" => "You are a Universal Donor! Your red blood cells can be given to anyone.\nYou can receive from: O- only.\nYour blood is extremely valuable in emergencies.",
            "O+" => "You can donate to: O+, A+, B+, AB+\nYou can receive from: O-, O+\nO+ is the most common blood type.",
            "A-" => "You can donate to: A-, A+, AB-, AB+\nYou can receive from: O-, A-",
            "A+" => "You can donate to: A+, AB+\nYou can receive from: O-, O+, A-, A+",
            "B-" => "You can donate to: B-, B+, AB-, AB+\nYou can receive from: O-, B-",
            "B+" => "You can donate to: B+, AB+\nYou can receive from: O-, O+, B-, B+",
            "AB-" => "You can donate to: AB-, AB+\nYou can receive from: O-, A-, B-, AB-\nYou are a Universal Plasma Donor!",
            "AB+" => "You are a Universal Recipient! You can receive red blood cells from anyone.\nYou can donate to: AB+ only.\nYou are also a Universal Plasma Donor.",
            _ => $"Compatibility information for {fullType} is not available."
        };
    }
}
