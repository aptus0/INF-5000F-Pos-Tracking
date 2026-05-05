using SamerHub.Core.Enums;

namespace SamerHub.Core.Contracts;

public sealed class CreateManualOrderRequest
{
    public string PlatformName { get; set; } = "Manual";
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public decimal TotalAmount { get; set; }
}
