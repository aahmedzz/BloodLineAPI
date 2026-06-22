using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.DonorEligibility.Queries.GetCooldownSettings;

public sealed record GetCooldownSettingsQuery : IRequest<Result<CooldownSettingsResult>>;
