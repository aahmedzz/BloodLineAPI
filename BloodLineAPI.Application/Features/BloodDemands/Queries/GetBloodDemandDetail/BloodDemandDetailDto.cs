using System;
using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.BloodDemands.Queries.GetBloodDemandDetail
{
    public record BloodDemandIssuanceHistoryDto(
        Guid IssuanceId,
        DateTime IssuedAt,
        string IssuedByName,
        string SerialNumber,
        string RecipientName,
        string NationalId,
        string? Phone,
        string Reason
    );

    public record BloodDemandDetailDto(
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
        DateTime CreatedAt,
        List<BloodDemandIssuanceHistoryDto> IssuanceHistory
    );
}
