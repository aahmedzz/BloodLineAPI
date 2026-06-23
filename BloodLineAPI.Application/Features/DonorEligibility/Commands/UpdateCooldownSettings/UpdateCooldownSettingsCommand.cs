using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.DonorEligibility.Commands.UpdateCooldownSettings;

public sealed record UpdateCooldownSettingsCommand(
    int WholeBloodMaleDays,
    int WholeBloodFemaleDays,
    int PlasmaDays,
    int PlateletsDays,
    int DefaultScreeningLockoutDays)
    : IRequest<Result<CooldownSettingsResult>>;
