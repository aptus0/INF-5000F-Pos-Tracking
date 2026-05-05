namespace SamerHub.Core.Entities;

public sealed class PrinterSettings
{
    public string KitchenPrinterName { get; set; } = string.Empty;
    public string CashPrinterName { get; set; } = string.Empty;
    public string DeliveryPrinterName { get; set; } = string.Empty;
    public bool UseEscPosForKitchen { get; set; } = true;
    public bool UseEscPosForCash { get; set; } = true;
    public bool UseEscPosForDelivery { get; set; } = true;
    public string LastTestResult { get; set; } = string.Empty;
    public DateTimeOffset? LastTestAtUtc { get; set; }
}
