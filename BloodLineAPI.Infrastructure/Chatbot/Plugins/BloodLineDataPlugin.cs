using System.ComponentModel;
using BloodLineAPI.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace BloodLineAPI.Infrastructure.Chatbot.Plugins;

public class BloodLineDataPlugin
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<BloodLineDataPlugin> _logger;
    private readonly IMemoryCache _cache;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BloodLineDataPlugin(
        IApplicationDbContext context, 
        ILogger<BloodLineDataPlugin> logger, 
        IMemoryCache cache,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _dateTimeProvider = dateTimeProvider;
    }

    [KernelFunction, Description("Gets a list of all currently active blood donation campaigns. Useful when the user asks about campaigns or where they can donate in mobile campaigns.")]
    public async Task<string> GetActiveBloodCampaignsAsync()
    {
        const string cacheKey = "monqez:campaigns:active";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            var campaigns = await _context.DonationCenters
                .AsNoTracking()
                .Where(c => c.Status == BloodLineAPI.Domain.Enums.CenterStatus.Active && c.CenterType == BloodLineAPI.Domain.Enums.CenterType.Campaign)
                .Select(c => new
                {
                    c.Name,
                    c.Location,
                    c.StartDate,
                    c.EndDate,
                    c.SupportedDonationTypes
                })
                .ToListAsync(cts.Token);

            if (campaigns.Count == 0)
            {
                return "There are no active blood donation campaigns at this time.";
            }

            var resultBuilder = new System.Text.StringBuilder("Active Campaigns:\n");
            foreach (var c in campaigns)
            {
                string endDateStr = c.EndDate.HasValue ? c.EndDate.Value.ToString("yyyy-MM-dd") : "Ongoing";
                resultBuilder.AppendLine($"- {c.Name} located in {c.Location}. Valid from {c.StartDate:yyyy-MM-dd} to {endDateStr}. Supported Donations: {c.SupportedDonationTypes}.");
            }

            var resultString = resultBuilder.ToString();
            
            _cache.Set(cacheKey, resultString, TimeSpan.FromMinutes(10));
            return resultString;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while fetching active blood campaigns.");
            return "Unable to fetch campaigns at this time. Please try again later.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching active blood campaigns.");
            return "Unable to fetch campaigns at this time. Please try again later.";
        }
    }

    [KernelFunction, Description("Gets the details and location of the main blood donation branch (Monqez Main Branch). Useful when the user asks where the main center is or where they can donate.")]
    public async Task<string> GetMainBranchInformationAsync()
    {
        const string cacheKey = "monqez:centers:main_branch";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            var mainBranch = await _context.DonationCenters
                .AsNoTracking()
                .Where(c => c.Status == BloodLineAPI.Domain.Enums.CenterStatus.Active && c.CenterType == BloodLineAPI.Domain.Enums.CenterType.MainBranch)
                .Select(c => new
                {
                    c.Name,
                    c.Location,
                    c.SupportedDonationTypes
                })
                .FirstOrDefaultAsync(cts.Token);

            if (mainBranch == null)
            {
                return "The main branch information is currently unavailable.";
            }

            var resultString = $"Main Branch: {mainBranch.Name}\nLocation: {mainBranch.Location}\nSupported Donations: {mainBranch.SupportedDonationTypes}";
            
            // Cache longer since main branch info rarely changes
            _cache.Set(cacheKey, resultString, TimeSpan.FromHours(1));
            return resultString;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while fetching main branch information.");
            return "Unable to fetch main branch information at this time. Please try again later.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching main branch information.");
            return "Unable to fetch main branch information at this time. Please try again later.";
        }
    }

    [KernelFunction, Description("Checks whether a donation center is open on a specific date and returns its working hours. Useful when the user asks 'is the branch open today?', 'what are the working hours?', 'is the center open on Friday?', or 'هل الفرع مفتوح بكره؟'. The date parameter should be in yyyy-MM-dd format.")]
    public async Task<string> GetCenterHoursAsync(
        [Description("The name or partial name of the donation center to look up. Use 'main' for the main branch.")] string centerName,
        [Description("The date to check in yyyy-MM-dd format. Use today's date if the user does not specify.")] string date)
    {
        if (!DateTime.TryParse(date, out var targetDate))
            return "Invalid date format. Please provide a date like 2026-05-09.";

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Find the center by name (fuzzy match) or default to main branch
            var isMainBranch = centerName.Contains("main", StringComparison.OrdinalIgnoreCase)
                            || centerName.Contains("رئيسي", StringComparison.OrdinalIgnoreCase);

            var center = await _context.DonationCenters
                .AsNoTracking()
                .Include(c => c.OpeningHours)
                .Include(c => c.CenterExclusions)
                .Where(c => c.Status == BloodLineAPI.Domain.Enums.CenterStatus.Active)
                .Where(c => isMainBranch
                    ? c.CenterType == BloodLineAPI.Domain.Enums.CenterType.MainBranch
                    : EF.Functions.Like(c.Name, $"%{centerName}%"))
                .FirstOrDefaultAsync(cts.Token);

            if (center == null)
                return $"No active center found matching '{centerName}'.";

            // Check for a date-specific exclusion (holiday, special hours, etc.)
            var exclusion = center.CenterExclusions
                .FirstOrDefault(e => e.Date.Date == targetDate.Date);

            var dayName = targetDate.ToString("dddd");

            if (exclusion != null)
            {
                if (exclusion.IsClosed)
                    return $"{center.Name}\nDate: {dayName}, {targetDate:yyyy-MM-dd}\nStatus: ❌ Closed\nReason: {exclusion.Reason}";

                // Special hours for that day
                var specialOpen = exclusion.SpecialOpeningTime ?? center.StartTime;
                var specialClose = exclusion.SpecialClosingTime ?? center.EndTime;
                return $"{center.Name}\nDate: {dayName}, {targetDate:yyyy-MM-dd}\nStatus: ✅ Open (Special Hours)\nHours: {FormatTime(specialOpen)} – {FormatTime(specialClose)}\nNote: {exclusion.Reason}";
            }

            // Check regular weekly schedule
            var daySchedule = center.OpeningHours
                .FirstOrDefault(h => h.DayOfWeek == targetDate.DayOfWeek);

            if (daySchedule != null)
            {
                if (daySchedule.IsClosed)
                    return $"{center.Name}\nDate: {dayName}, {targetDate:yyyy-MM-dd}\nStatus: ❌ Closed (Regular weekly closure)";

                return $"{center.Name}\nDate: {dayName}, {targetDate:yyyy-MM-dd}\nStatus: ✅ Open\nHours: {FormatTime(daySchedule.OpeningTime)} – {FormatTime(daySchedule.ClosingTime)}";
            }

            // Fallback to center defaults
            return $"{center.Name}\nDate: {dayName}, {targetDate:yyyy-MM-dd}\nStatus: ✅ Open\nHours: {FormatTime(center.StartTime)} – {FormatTime(center.EndTime)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching center hours for '{CenterName}' on {Date}.", centerName, date);
            return "Unable to fetch center hours at this time. Please try again later.";
        }
    }

    private string FormatTime(TimeSpan time)
    {
        var dateTime = _dateTimeProvider.LocalNow.Date.Add(time);
        return dateTime.ToString("hh:mm tt");
    }
}
