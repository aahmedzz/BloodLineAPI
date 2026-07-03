using MediatR;
using System;

namespace BloodLineAPI.Application.Features.BloodDemands.Commands.CancelBloodDemand
{
    public sealed record CancelBloodDemandCommand(Guid Id) : IRequest;
}
