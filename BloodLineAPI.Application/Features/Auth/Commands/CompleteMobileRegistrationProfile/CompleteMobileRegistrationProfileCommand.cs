using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace BloodLineAPI.Application.Features.Auth.Commands.CompleteMobileRegistrationProfile;

public sealed record CompleteMobileRegistrationProfileCommand(
    DateOnly DateOfBirth,
    Gender Gender,
    BloodGroupName BloodGroupName,
    RhFactor RhFactor,
    decimal? WeightKg) : IRequest<Result<DonorAuthResponse>>
{
    [JsonIgnore]
    public Guid UserId { get; init; }
}
