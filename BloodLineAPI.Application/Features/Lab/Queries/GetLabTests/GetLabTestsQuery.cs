using MediatR;

namespace BloodLineAPI.Application.Features.Lab.Queries.GetLabTests;

public sealed record GetLabTestsQuery(
    int Page = 1,
    int Limit = 10,
    string? Status = null,
    string? Search = null,
    string? BloodType = null) : IRequest<GetLabTestsResult>;