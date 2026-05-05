using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfFiscalSettingsRepository(JsonSettingsStore store) : IFiscalSettingsRepository
{
    private const string Key = "settings.fiscal";

    public Task<FiscalSettings> GetAsync(CancellationToken ct) =>
        store.GetAsync(Key, RepositoryDefaults.CreateFiscalSettings, ct);

    public Task<FiscalSettings> UpdateAsync(FiscalSettings settings, CancellationToken ct) =>
        store.SaveAsync(Key, settings, ct);
}
