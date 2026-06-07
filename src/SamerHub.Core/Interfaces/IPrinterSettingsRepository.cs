using SamerHub.Core.Entities;

namespace SamerHub.Core.Interfaces;

public interface IPrinterSettingsRepository
{
    Task<PrinterSettings> GetAsync(CancellationToken cancellationToken);
    Task<PrinterSettings> UpdateAsync(PrinterSettings settings, CancellationToken cancellationToken);
}
