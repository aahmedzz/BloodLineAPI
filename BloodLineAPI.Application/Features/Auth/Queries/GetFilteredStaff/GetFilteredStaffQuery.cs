using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Auth.Queries.GetFilteredStaff;

public record GetFilteredStaffQuery(
    int Page = 1,
    int Limit = 10,
    string? Search = null,
    string? Role = null,
    string? Status = null) : IRequest<Result<PaginatedStaffResult>>;
