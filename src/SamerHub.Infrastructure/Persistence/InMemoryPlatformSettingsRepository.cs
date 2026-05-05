using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class InMemoryPlatformSettingsRepository : IPlatformSettingsRepository
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private PlatformApiSettings _settings = new();

    public async Task<PlatformApiSettings> GetAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return Clone(_settings);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<PlatformApiSettings> UpdateAsync(PlatformApiSettings settings, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _settings = Clone(settings);
            return Clone(_settings);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static PlatformApiSettings Clone(PlatformApiSettings settings)
        => new()
        {
            TrendyolGo = Clone(settings.TrendyolGo),
            Getir = Clone(settings.Getir),
            Yemeksepeti = Clone(settings.Yemeksepeti)
        };

    private static PlatformConnectionSettings Clone(PlatformConnectionSettings settings)
        => new()
        {
            IsEnabled = settings.IsEnabled,
            BaseUrl = settings.BaseUrl,
            MerchantId = settings.MerchantId,
            StoreId = settings.StoreId,
            ApiKey = settings.ApiKey,
            ApiSecret = settings.ApiSecret,
            LastTestResult = settings.LastTestResult,
            LastTestAtUtc = settings.LastTestAtUtc
        };
}
