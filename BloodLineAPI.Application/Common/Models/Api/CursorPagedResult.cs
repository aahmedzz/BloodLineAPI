namespace BloodLineAPI.Application.Common.Models;

public sealed record CursorPagedResult<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore);
