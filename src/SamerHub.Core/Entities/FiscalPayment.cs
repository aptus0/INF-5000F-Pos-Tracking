namespace SamerHub.Core.Entities;

public sealed class FiscalPayment
{
    public Guid Id { get; set; }
    public Guid FiscalReceiptId { get; set; }
    public string PaymentCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
