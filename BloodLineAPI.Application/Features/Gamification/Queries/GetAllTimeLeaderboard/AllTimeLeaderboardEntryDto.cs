namespace BloodLineAPI.Application.Features.Gamification.Queries.GetAllTimeLeaderboard;

public sealed record AllTimeLeaderboardEntryDto(
    Guid DonorId,
    string FullName,
    string? District,
    string? Area,
    int Points,
    int Rank);
