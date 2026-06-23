using System.Collections.Generic;
using MediatR;

namespace BloodLineAPI.Application.Features.Inventory.Queries.GetInventoryThresholds;

public sealed record GetInventoryThresholdsQuery() : IRequest<Dictionary<string, int>>;
