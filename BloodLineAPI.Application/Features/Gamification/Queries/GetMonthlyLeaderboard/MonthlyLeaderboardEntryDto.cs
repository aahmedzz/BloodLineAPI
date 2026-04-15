namespace BloodLineAPI.Application.Features.Gamification.Queries.GetMonthlyLeaderboard;

public sealed record MonthlyLeaderboardEntryDto(
    Guid DonorId,
    string FullName,
    string? District,
    string? Area,
    int Points,
    int Rank);
