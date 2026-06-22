using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Domain.Common;

namespace BloodLineAPI.Application.Common.Interfaces;

public interface IDynamicSettingsService
{
    Task<DynamicSystemSettings> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateSettingsAsync(DynamicSystemSettings settings, CancellationToken cancellationToken = default);
}
