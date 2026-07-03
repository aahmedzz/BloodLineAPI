using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.BloodDemands.Queries.GetBloodDemands
{
    public record BloodDemandDto(
        Guid Id,
        DateTime RequestDate,
        string BloodType,
        string RequesterName,
        int RequestedUnits,
        int IssuedUnits,
        int RemainingUnits,
        string Priority,
        string Status,
        string? Notes,
        DateTime CreatedAt
    );

    public record GetBloodDemandsResult(
        List<BloodDemandDto> Items,
        int Page,
        int Limit,
        int TotalCount,
        int TotalPages
    );
}
