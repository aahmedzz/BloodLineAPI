using System.Collections.Generic;

namespace BloodLineAPI.Controllers.V1.System.Requests;

public class UpdateInventoryThresholdsRequest
{
    public Dictionary<string, int> Thresholds { get; set; } = new();
}
