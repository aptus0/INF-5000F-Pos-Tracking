using SamerHub.Core.Entities;

namespace SamerHub.Infrastructure.Persistence;

internal static class RepositoryDefaults
{
    public static PosSettings CreatePosSettings() => new();

    public static PrinterSettings CreatePrinterSettings() => new();

    public static PlatformApiSettings CreatePlatformSettings() => new();

    public static FiscalSettings CreateFiscalSettings() => new()
    {
        IsEnabled = false,
        Mode = "Fake",
        ReceiptTrigger = "AtPayment",
        VatMappings = new List<VatDepartmentMapping>
        {
            new() { VatRate = 0, DepartmentCode = "00", Description = "KDV Yok" },
            new() { VatRate = 1, DepartmentCode = "01", Description = "Dusuk KDV" },
            new() { VatRate = 10, DepartmentCode = "02", Description = "Yemek / Gida" },
            new() { VatRate = 20, DepartmentCode = "03", Description = "Genel Urun" }
        },
        PaymentMappings = new List<PaymentTypeMapping>
        {
            new() { SamerPaymentType = "CashOnDoor", FiscalPaymentCode = "CASH" },
            new() { SamerPaymentType = "CardOnDoor", FiscalPaymentCode = "CARD" },
            new() { SamerPaymentType = "PlatformOnline", FiscalPaymentCode = "OTHER" }
        }
    };

    public static List<DiningTable> CreateTables() =>
    [
        new() { Name = "Masa 1", Area = "Salon", Capacity = 4, Status = "Available" },
        new() { Name = "Masa 2", Area = "Salon", Capacity = 4, Status = "Occupied", IsOccupied = true, ActiveOrderTotal = 420 },
        new() { Name = "Teras 1", Area = "Teras", Capacity = 2, Status = "Reserved" },
        new() { Name = "VIP 1", Area = "VIP", Capacity = 6, Status = "Available" }
    ];

    public static List<Category> CreateCategories() =>
    [
        new() { Name = "Izgara", SortOrder = 1 },
        new() { Name = "Icecek", SortOrder = 2 },
        new() { Name = "Tatli", SortOrder = 3 }
    ];

    public static List<Product> CreateProducts() =>
    [
        new()
        {
            Name = "Adana Kebap",
            CategoryName = "Izgara",
            UnitPrice = 320,
            KdvOrani = 10,
            YnokcDepartmani = "02",
            UnitType = "ADET",
            MaliUrunKodu = "ADANA-001",
            IsActive = true
        },
        new()
        {
            Name = "Kola",
            CategoryName = "Icecek",
            UnitPrice = 80,
            KdvOrani = 20,
            YnokcDepartmani = "03",
            UnitType = "ADET",
            MaliUrunKodu = "KOLA-001",
            IsActive = true
        },
        new()
        {
            Name = "Kunefe",
            CategoryName = "Tatli",
            UnitPrice = 190,
            KdvOrani = 10,
            YnokcDepartmani = "02",
            UnitType = "ADET",
            MaliUrunKodu = "KUNEFE-001",
            IsActive = true
        }
    ];
}
