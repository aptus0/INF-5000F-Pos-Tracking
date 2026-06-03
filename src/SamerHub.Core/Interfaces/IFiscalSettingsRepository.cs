namespace SamerHub.Core.Interfaces;

using Entities;

public interface IFiscalSettingsRepository
{
    Task<FiscalSettings> GetAsync(CancellationToken ct);
    Task<FiscalSettings> UpdateAsync(FiscalSettings settings, CancellationToken ct);
}
