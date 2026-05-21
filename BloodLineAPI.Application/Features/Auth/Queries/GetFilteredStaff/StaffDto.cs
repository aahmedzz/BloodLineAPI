using System;

namespace BloodLineAPI.Application.Features.Auth.Queries.GetFilteredStaff;

public record StaffDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string NationalId,
    string Phone,
    string Address,
    string City,
    string Status,
    string CreatedAt);
