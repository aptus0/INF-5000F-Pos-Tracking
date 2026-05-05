namespace SamerHub.Core.Entities;

public sealed class FiscalSettings
{
    public bool IsEnabled { get; set; } = false;
    public string Mode { get; set; } = "Fake";
    public string ReceiptTrigger { get; set; } = "AtPayment";
    public bool DeviceTestEnabled { get; set; }
    public string LastDeviceResponse { get; set; } = string.Empty;
    public IList<VatDepartmentMapping> VatMappings { get; set; } = [];
    public IList<PaymentTypeMapping> PaymentMappings { get; set; } = [];
}
