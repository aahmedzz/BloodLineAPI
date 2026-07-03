using MediatR;
using System;

namespace BloodLineAPI.Application.Features.BloodDemands.Queries.GetBloodDemandDetail
{
    public sealed record GetBloodDemandDetailQuery(Guid Id) : IRequest<BloodDemandDetailDto?>;
}
