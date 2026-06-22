using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using BloodLineAPI.Domain.Common;
using MediatR;

namespace BloodLineAPI.Application.Features.DonorEligibility.Commands.UpdateCooldownSettings;

public sealed class UpdateCooldownSettingsCommandHandler(IDynamicSettingsService dynamicSettingsService)
    : IRequestHandler<UpdateCooldownSettingsCommand, Result<CooldownSettingsResult>>
{
    public async Task<Result<CooldownSettingsResult>> Handle(
        UpdateCooldownSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = new DynamicSystemSettings
        {
            WholeBloodMaleDays = request.WholeBloodMaleDays,
            WholeBloodFemaleDays = request.WholeBloodFemaleDays,
            PlasmaDays = request.PlasmaDays,
            PlateletsDays = request.PlateletsDays,
            DefaultScreeningLockoutDays = request.DefaultScreeningLockoutDays
        };

        await dynamicSettingsService.UpdateSettingsAsync(settings, cancellationToken);

        var result = new CooldownSettingsResult(
            WholeBloodMaleDays: settings.WholeBloodMaleDays,
            WholeBloodFemaleDays: settings.WholeBloodFemaleDays,
            PlasmaDays: settings.PlasmaDays,
            PlateletsDays: settings.PlateletsDays,
            DefaultScreeningLockoutDays: settings.DefaultScreeningLockoutDays
        );

        return Result<CooldownSettingsResult>.Success(result);
    }
}
