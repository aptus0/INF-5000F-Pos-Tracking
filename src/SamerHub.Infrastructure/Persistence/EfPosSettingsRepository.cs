using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfPosSettingsRepository(JsonSettingsStore store) : IPosSettingsRepository
{
    private const string Key = "settings.pos";

    public Task<PosSettings> GetAsync(CancellationToken cancellationToken) =>
        store.GetAsync(Key, RepositoryDefaults.CreatePosSettings, cancellationToken);

    public Task<PosSettings> UpdateAsync(PosSettings settings, CancellationToken cancellationToken) =>
        store.SaveAsync(Key, settings, cancellationToken);

    public async Task<PosSettings> UpdateConnectionResultAsync(bool isSuccessful, long? roundTripMilliseconds, string errorMessage, CancellationToken cancellationToken)
    {
        var settings = await GetAsync(cancellationToken);
        settings.LastConnectionSucceeded = isSuccessful;
        settings.LastConnectionAtUtc = DateTimeOffset.UtcNow;
        settings.LastRoundTripMilliseconds = roundTripMilliseconds;
        settings.LastError = errorMessage;
        return await UpdateAsync(settings, cancellationToken);
    }
}
