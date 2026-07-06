using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Gamification.Queries.GetAllTimeLeaderboard;

public sealed record AllTimeLeaderboardResponseDto(
    IReadOnlyList<AllTimeLeaderboardEntryDto> Entries,
    AllTimeLeaderboardEntryDto? MyEntry,
    bool HasNextPage
);
