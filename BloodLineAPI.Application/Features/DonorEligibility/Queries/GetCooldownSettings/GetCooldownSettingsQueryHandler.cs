using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonorEligibility.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.DonorEligibility.Queries.GetCooldownSettings;

public sealed class GetCooldownSettingsQueryHandler(IDynamicSettingsService dynamicSettingsService)
    : IRequestHandler<GetCooldownSettingsQuery, Result<CooldownSettingsResult>>
{
    public async Task<Result<CooldownSettingsResult>> Handle(
        GetCooldownSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await dynamicSettingsService.GetSettingsAsync(cancellationToken);
        
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
