using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfPrinterSettingsRepository(JsonSettingsStore store) : IPrinterSettingsRepository
{
    private const string Key = "settings.printers";

    public Task<PrinterSettings> GetAsync(CancellationToken cancellationToken) =>
        store.GetAsync(Key, RepositoryDefaults.CreatePrinterSettings, cancellationToken);

    public Task<PrinterSettings> UpdateAsync(PrinterSettings settings, CancellationToken cancellationToken) =>
        store.SaveAsync(Key, settings, cancellationToken);
}
