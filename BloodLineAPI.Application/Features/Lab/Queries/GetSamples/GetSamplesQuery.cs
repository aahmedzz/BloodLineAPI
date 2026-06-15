using MediatR;

namespace BloodLineAPI.Application.Features.Lab.Queries.GetSamples;

public sealed record GetSamplesQuery(
    int Page = 1,
    int Limit = 100,
    string? Search = null,
    string? Status = null,
    string? BloodType = null) : IRequest<GetSamplesResult>;