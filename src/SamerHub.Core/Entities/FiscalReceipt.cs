using SamerHub.Core.Enums;

namespace SamerHub.Core.Entities;

public sealed class FiscalReceipt
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public FiscalReceiptStatus Status { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public string ZNo { get; set; } = string.Empty;
    public string EcrNo { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal TotalVat { get; set; }
    public string RawResponse { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public IList<FiscalReceiptItem> Items { get; set; } = [];
    public IList<FiscalPayment> Payments { get; set; } = [];
}
