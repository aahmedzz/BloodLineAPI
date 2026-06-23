using System.Collections.Generic;
using MediatR;

namespace BloodLineAPI.Application.Features.Inventory.Commands.UpdateInventoryThresholds;

public sealed record UpdateInventoryThresholdsCommand(Dictionary<string, int> Thresholds) : IRequest<Dictionary<string, int>>;
