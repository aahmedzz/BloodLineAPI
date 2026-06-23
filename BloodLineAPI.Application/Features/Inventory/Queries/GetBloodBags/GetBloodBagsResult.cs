namespace BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBags;

public sealed record BloodBagDto(
    Guid Id,
    string BagCode,
    string BloodType,
    string DonationType,
    string? DonorCode,
    string CollectedDate,
    string ExpiryDate,
    string Status,
    decimal Volume,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? IssuedAt,
    Guid? IssuedById,
    string? IssuedByName,
    DateTime? DisposedAt,
    Guid? DisposedById,
    string? DisposedByName,
    string? DisposeReason,
    string? DisposeNotes
);

public sealed record GetBloodBagsResult(
    IEnumerable<BloodBagDto> Items,
    int Page,
    int Limit,
    int Total,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
);
