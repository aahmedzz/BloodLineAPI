using MediatR;

namespace BloodLineAPI.Application.Features.BloodDemands.Queries.GetBloodDemandsDashboard
{
    public sealed record GetBloodDemandsDashboardQuery() : IRequest<BloodDemandsDashboardResult>;

    public record BloodDemandsDashboardResult(
        int Total,
        int Pending,
        int PartiallyFulfilled,
        int Fulfilled
    );
}
