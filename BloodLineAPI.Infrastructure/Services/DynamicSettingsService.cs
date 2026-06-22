using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Domain.Common;
using Microsoft.AspNetCore.Hosting;

namespace BloodLineAPI.Infrastructure.Services;

public class DynamicSettingsService : IDynamicSettingsService
{
    private readonly string _filePath;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private DynamicSystemSettings? _cachedSettings;

    public DynamicSettingsService(IWebHostEnvironment environment)
    {
        _filePath = Path.Combine(environment.WebRootPath, "system_settings.json");
    }

    public async Task<DynamicSystemSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            if (!File.Exists(_filePath))
            {
                var defaults = new DynamicSystemSettings
                {
                    WholeBloodMaleDays = 90,
                    WholeBloodFemaleDays = 120,
                    PlasmaDays = 28,
                    PlateletsDays = 7,
                    DefaultScreeningLockoutDays = 7
                };

                await SaveSettingsInternalAsync(defaults, cancellationToken);
                _cachedSettings = defaults;
                return defaults;
            }

            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            var settings = JsonSerializer.Deserialize<DynamicSystemSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new DynamicSystemSettings();

            _cachedSettings = settings;
            return settings;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateSettingsAsync(DynamicSystemSettings settings, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await SaveSettingsInternalAsync(settings, cancellationToken);
            _cachedSettings = settings;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task SaveSettingsInternalAsync(DynamicSystemSettings settings, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_filePath, json, cancellationToken);
    }
}
