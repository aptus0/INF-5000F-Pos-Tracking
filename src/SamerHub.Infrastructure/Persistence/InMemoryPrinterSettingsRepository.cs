using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class InMemoryPrinterSettingsRepository : IPrinterSettingsRepository
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private PrinterSettings _settings = new();

    public async Task<PrinterSettings> GetAsync(CancellationToken cancellationToken)
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

    public async Task<PrinterSettings> UpdateAsync(PrinterSettings settings, CancellationToken cancellationToken)
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

    private static PrinterSettings Clone(PrinterSettings settings)
        => new()
        {
            KitchenPrinterName = settings.KitchenPrinterName,
            CashPrinterName = settings.CashPrinterName,
            DeliveryPrinterName = settings.DeliveryPrinterName,
            UseEscPosForKitchen = settings.UseEscPosForKitchen,
            UseEscPosForCash = settings.UseEscPosForCash,
            UseEscPosForDelivery = settings.UseEscPosForDelivery,
            LastTestResult = settings.LastTestResult,
            LastTestAtUtc = settings.LastTestAtUtc
        };
}
