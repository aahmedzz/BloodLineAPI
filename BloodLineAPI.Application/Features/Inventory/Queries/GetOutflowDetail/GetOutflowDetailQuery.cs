using System;
using MediatR;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetOutflowDetail;

public sealed record GetOutflowDetailQuery(Guid Id) : IRequest<GetOutflowDetailResult?>;
