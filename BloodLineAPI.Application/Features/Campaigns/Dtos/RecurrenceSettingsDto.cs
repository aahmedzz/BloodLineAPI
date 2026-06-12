using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Campaigns.Dtos;

public record RecurrenceSettingsDto(
    bool Enabled,
    string Type, // "none" | "daily" | "weekly" | "monthly" | "custom"
    IReadOnlyList<int>? WeekDays, // [0 = Sunday, 1 = Monday, ..., 6 = Saturday]
    string? EndDate // "YYYY-MM-DD" or null
);
