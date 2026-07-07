using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfPlatformSettingsRepository(JsonSettingsStore store) : IPlatformSettingsRepository
{
    private const string Key = "settings.platforms";

    public Task<PlatformApiSettings> GetAsync(CancellationToken cancellationToken) =>
        store.GetAsync(Key, RepositoryDefaults.CreatePlatformSettings, cancellationToken);

    public Task<PlatformApiSettings> UpdateAsync(PlatformApiSettings settings, CancellationToken cancellationToken) =>
        store.SaveAsync(Key, settings, cancellationToken);
}
