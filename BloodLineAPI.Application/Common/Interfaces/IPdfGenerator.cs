using System;
using System.Collections.Generic;
using BloodLineAPI.Application.Features.Inventory.Queries.GetOutflowHistory;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IPdfGenerator
{
    byte[] GenerateOutflowReport(List<OutflowListDto> items, string performedByName, DateTime generatedAt);
}
