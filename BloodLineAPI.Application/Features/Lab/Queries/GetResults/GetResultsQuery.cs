using MediatR;

namespace BloodLineAPI.Application.Features.Lab.Queries.GetResults;

public sealed record GetResultsQuery(
    int Page = 1,
    int Limit = 100,
    string? Search = null,
    string? BloodType = null,
    string? Outcome = null) : IRequest<GetResultsResult>;