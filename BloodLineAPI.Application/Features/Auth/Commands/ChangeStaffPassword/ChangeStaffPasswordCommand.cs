using BloodLineAPI.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Auth.Commands.ChangeStaffPassword;

public sealed record ChangeStaffPasswordCommand(
    string CurrentPassword,
    string NewPassword
) : IRequest<Result<string>>
{
    [JsonIgnore]
    public Guid UserId { get; init; }
}
