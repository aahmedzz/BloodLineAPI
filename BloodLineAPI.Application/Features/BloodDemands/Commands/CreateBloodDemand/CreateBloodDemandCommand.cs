using BloodLineAPI.Domain.Enums;
using MediatR;

namespace BloodLineAPI.Application.Features.BloodDemands.Commands.CreateBloodDemand
{
    public sealed record CreateBloodDemandCommand(
        byte BloodTypeId,
        string RequesterName,
        int RequestedUnits,
        BloodDemandPriority Priority,
        string? Notes
    ) : IRequest<Guid>;
}
