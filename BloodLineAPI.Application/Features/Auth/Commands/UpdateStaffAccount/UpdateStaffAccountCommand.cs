using BloodLineAPI.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Auth.Commands.UpdateStaffAccount;

public sealed record UpdateStaffAccountCommand(
    string? Name,
    string? Email,
    string? Role,
    string? NationalId,
    string? Phone,
    string? Address,
    string? City
) : IRequest<Result<Guid>>
{
    [JsonIgnore]
    public Guid StaffId { get; init; }
}
