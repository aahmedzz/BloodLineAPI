using System.Collections.Generic;

namespace BloodLineAPI.Application.Features.Auth.Queries.GetFilteredStaff;

public record PaginatedStaffResult(
    IReadOnlyList<StaffDto> Items,
    int Total,
    int Page,
    int Limit);
