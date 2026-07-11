using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class InMemoryFiscalSettingsRepository : IFiscalSettingsRepository
{
    private FiscalSettings _settings;

    public InMemoryFiscalSettingsRepository()
    {
        _settings = new FiscalSettings
        {
            IsEnabled = false,
            Mode = "Fake",
            ReceiptTrigger = "AtPayment",
            VatMappings = new List<VatDepartmentMapping>
            {
                new() { VatRate = 0, DepartmentCode = "00", Description = "KDV Yok" },
                new() { VatRate = 1, DepartmentCode = "01", Description = "Düşük KDV" },
                new() { VatRate = 10, DepartmentCode = "02", Description = "Yemek / Gıda" },
                new() { VatRate = 20, DepartmentCode = "03", Description = "Genel Ürün" }
            },
            PaymentMappings = new List<PaymentTypeMapping>
            {
                new() { SamerPaymentType = "CashOnDoor", FiscalPaymentCode = "CASH" },
                new() { SamerPaymentType = "CardOnDoor", FiscalPaymentCode = "CARD" },
                new() { SamerPaymentType = "PlatformOnline", FiscalPaymentCode = "OTHER" }
            }
        };
    }

    public Task<FiscalSettings> GetAsync(CancellationToken ct)
        => Task.FromResult(_settings);

    public Task<FiscalSettings> UpdateAsync(FiscalSettings settings, CancellationToken ct)
    {
        _settings = settings;
        return Task.FromResult(_settings);
    }
}
