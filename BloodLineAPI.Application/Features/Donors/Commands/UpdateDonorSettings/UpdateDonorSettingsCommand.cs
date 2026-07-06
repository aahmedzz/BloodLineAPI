using System.Text.Json.Serialization;
using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Donors.Commands.UpdateDonorSettings;

public sealed record UpdateDonorSettingsCommand(
    [property: JsonIgnore] Guid UserId, // injected from JWT claim — never from request body
    bool AllowLeaderboardVisibility
) : IRequest<Result<string>>;
