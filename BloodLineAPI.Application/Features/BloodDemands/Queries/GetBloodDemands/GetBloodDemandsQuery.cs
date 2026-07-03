using MediatR;

namespace BloodLineAPI.Application.Features.BloodDemands.Queries.GetBloodDemands
{
    public sealed record GetBloodDemandsQuery(
        int Page = 1,
        int Limit = 10,
        string? Search = null,
        string? Status = null,
        string? BloodType = null,
        string? Priority = null
    ) : IRequest<GetBloodDemandsResult>;
}
