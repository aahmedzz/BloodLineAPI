using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetMonthlyLeaderboard;

public sealed record MonthlyLeaderboardResponseDto(
    IReadOnlyList<MonthlyLeaderboardEntryDto> Entries,
    MonthlyLeaderboardEntryDto? MyEntry,
    bool HasNextPage
);
