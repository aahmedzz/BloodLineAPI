using MediatR;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetBloodBags;

public sealed record GetBloodBagsQuery(
    int Page = 1,
    int Limit = 10,
    string? Search = null,
    string? BloodType = null,
    string? BloodTypes = null,
    string? DonationType = null,
    string? Status = null,
    string? SortBy = null,
    string? SortOrder = null) : IRequest<GetBloodBagsResult>;
